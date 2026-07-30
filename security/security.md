# Security Review — PDF Form Filler

## Code Changes Made to UsefulPdfNet Engine (forked from iTextSharp-LGPL-Core)

The following changes were applied to the [itextsharp.csproj](./resources/iTextSharp-LGPL-Core/src/core/itextsharp.csproj) source (rebranded as UsefulPdfNet 10.0.1):

| Change | Original | Updated | Reason |
|--------|----------|---------|--------|
| Target framework | `netstandard2.0;net45;net40` | `netstandard2.0` only | net40/net45 require legacy .NET Framework SDK; not needed for .NET 10 |
| System.Drawing.Common | `4.7.0` | `8.0.0` | Fixes **CVE-2021-24112** (GHSA-rxg9-xrhp-64gj) — critical remote code execution vulnerability |
| GeneratePackageOnBuild | `true` | `false` | Not packaging as NuGet; built as project reference |
| NoWarn | *(none)* | `CS0612;CS0618` | Suppress obsolete BouncyCastle API warnings in third-party source |
| Removed dead ItemGroup | `net40\|net45` → BouncyCastle 1.8.6.1 | Deleted | Unreachable conditional; only netstandard2.0 is targeted |
| Removed duplicate reference | Second `System.Text.Encoding.CodePages` block | Deleted | Duplicated package reference |

---

## Security Improvements Needed

### 🔴 Critical — Path Traversal via `pdfPath` Hidden Field

**File:** [Index.cshtml.cs](./Pages/Index.cshtml.cs) (line 75)  
**Issue:** The `pdfPath` value comes from a hidden form field that a malicious user could modify to point to any file on disk. The `OnPostDownload` handler reads whatever path is submitted and processes it through `PdfReader`.

```csharp
var pdfPath = Request.Form["pdfPath"].FirstOrDefault();
// ... then passes directly to PdfReader
```

**Recommendation:** Validate that `pdfPath` resides within the expected `wwwroot/uploads/` directory using `Path.GetFullPath()` canonicalization:

```csharp
var uploadsDir = Path.GetFullPath(Path.Combine(_env.WebRootPath, "uploads"));
var fullPath = Path.GetFullPath(pdfPath);
if (!fullPath.StartsWith(uploadsDir, StringComparison.OrdinalIgnoreCase))
{
    ErrorMessage = "Invalid file path.";
    return Page();
}
```

---

### 🔴 Critical — Path Traversal via `path` Query String

**File:** [Index.cshtml.cs](./Pages/Index.cshtml.cs) (line 28)  
**Issue:** The `OnGet` handler accepts an arbitrary file path from the query string and passes it to `PdfReader`:

```csharp
var path = Request.Query["path"].FirstOrDefault();
if (!string.IsNullOrEmpty(path) && System.IO.File.Exists(path))
{
    LoadPdfFields(path);  // Opens any file on disk
}
```

**Recommendation:** Apply the same uploads-directory validation, or remove this feature entirely if not needed.

---

### 🟡 High — No Upload File Size Limit

**File:** [Program.cs](./Program.cs)  
**Issue:** No request size limit is configured. A malicious user could upload extremely large files to consume disk space or memory.

**Recommendation:** Add Kestrel request body size limits:

```csharp
builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 50 * 1024 * 1024; // 50 MB max
});
```

---

### 🟡 High — Uploaded Files Never Cleaned Up

**File:** [Index.cshtml.cs](./Pages/Index.cshtml.cs) (line 55)  
**Issue:** Files are saved to `wwwroot/uploads/` with unique GUID names but are never deleted. Over time this will consume disk space, and the files are served as static files.

**Recommendation:**
- Move uploads **outside** `wwwroot/` (e.g., a `temp/` folder) so they are not publicly accessible via URL
- Add a background cleanup service or delete files after download
- Set `StaticFileOptions` to exclude the uploads directory

---

### 🟡 High — Uploaded PDFs Served as Static Files

**File:** [Program.cs](./Program.cs) (line 22)  
**Issue:** `app.UseStaticFiles()` serves everything in `wwwroot/`, including `wwwroot/uploads/`. Anyone with the GUID filename can download any uploaded PDF directly.

**Recommendation:** Either:
- Move uploads outside `wwwroot/` (preferred), or
- Configure `StaticFileOptions` to exclude the uploads path:

```csharp
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        if (ctx.File.PhysicalPath?.Contains("uploads") == true)
        {
            ctx.Context.Response.StatusCode = 403;
            ctx.Context.Response.ContentLength = 0;
            ctx.Context.Response.Body = Stream.Null;
        }
    }
});
```

---

### 🟡 Medium — Filename Injection in Upload

**File:** [Index.cshtml.cs](./Pages/Index.cshtml.cs) (line 54)  
**Issue:** The original filename from the user is included in the saved path: `$"{Guid.NewGuid():N}_{pdfFile.FileName}"`. If the filename contains path separators or special characters, it could cause issues.

**Recommendation:** Sanitize the filename:

```csharp
var safeName = Path.GetFileName(pdfFile.FileName); // strips directory components
var uniqueName = $"{Guid.NewGuid():N}_{safeName}";
```

---

### 🟡 Medium — Exception Details Exposed to Users

**File:** [Index.cshtml.cs](./Pages/Index.cshtml.cs) (lines 67, 112)  
**Issue:** Raw `ex.Message` is shown to the user. In production, exception messages can leak internal paths, stack details, or library internals.

```csharp
ErrorMessage = $"Error processing PDF: {ex.Message}";
```

**Recommendation:** Log the full exception server-side and show a generic message to the user:

```csharp
_logger.LogError(ex, "Error processing PDF upload");
ErrorMessage = "An error occurred processing the PDF. Please try again.";
```

---

### 🟢 Low — Content-Type Validation

**File:** [Index.cshtml.cs](./Pages/Index.cshtml.cs) (line 43)  
**Issue:** Only the file extension is checked (`.pdf`). A malicious file could be renamed to `.pdf`.

**Recommendation:** Also validate the Content-Type and/or check the file's magic bytes:

```csharp
if (pdfFile.ContentType != "application/pdf")
{
    ErrorMessage = "Invalid file type.";
    return Page();
}
// Optional: check first 5 bytes == "%PDF-"
```

---

### 🟢 Low — Antiforgery Token

**File:** [Index.cshtml](./Pages/Index.cshtml)  
**Status:** ✅ Already handled — ASP.NET Core Razor Pages includes antiforgery tokens automatically via the `asp-page-handler` tag helper. CSRF protection is active by default.

---

### 🟢 Low — HTTPS Redirection

**File:** [Program.cs](./Program.cs) (line 21)  
**Status:** ✅ Already configured — `app.UseHttpsRedirection()` and `app.UseHsts()` are in place.

---

### 🟢 Low — XSS via Razor Output

**File:** [Index.cshtml](./Pages/Index.cshtml)  
**Status:** ✅ Mitigated — Razor `@` expressions are HTML-encoded by default. Field names and values rendered via `@field.Name`, `@field.CurrentValue`, etc. are auto-escaped.

---

## Dependency Vulnerability Summary

| Package | Version | Status |
|---------|---------|--------|
| System.Drawing.Common | 8.0.0 | ✅ Patched (upgraded from vulnerable 4.7.0) |
| Portable.BouncyCastle | 1.8.1.3 | ⚠️ Outdated — latest is 2.x. No critical CVEs but consider upgrading |
| System.Text.Encoding.CodePages | 4.7.1 | ✅ No known vulnerabilities |

---

## Summary of Recommendations (Priority Order)

| # | Priority | Issue | Fix |
|---|----------|-------|-----|
| 1 | 🔴 Critical | Path traversal via `pdfPath` hidden field | Validate path is within uploads directory |
| 2 | 🔴 Critical | Path traversal via `path` query string | Validate or remove query string path loading |
| 3 | 🟡 High | No upload size limit | Configure `FormOptions.MultipartBodyLengthLimit` |
| 4 | 🟡 High | Uploads never cleaned up | Add cleanup service or delete after download |
| 5 | 🟡 High | Uploads served as static files | Move uploads outside `wwwroot/` |
| 6 | 🟡 Medium | Filename injection risk | Sanitize with `Path.GetFileName()` |
| 7 | 🟡 Medium | Exception details exposed | Log internally, show generic message |
| 8 | 🟢 Low | Extension-only file validation | Add Content-Type and magic byte checks |
| 9 | 🟢 Low | BouncyCastle outdated | Consider upgrading to 2.x |
