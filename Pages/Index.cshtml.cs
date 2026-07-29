using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PdfSharp.Models;
using PdfSharp.Services;

namespace PdfSharp.Pages;

public class IndexModel : PageModel
{
    private readonly PdfFormService _pdfService;
    private readonly IWebHostEnvironment _env;

    public IndexModel(PdfFormService pdfService, IWebHostEnvironment env)
    {
        _pdfService = pdfService;
        _env = env;
    }

    public bool HasUploadedPdf { get; set; }
    public string UploadedFileName { get; set; } = string.Empty;
    public string UploadedFilePath { get; set; } = string.Empty;
    public List<FormFieldInfo> FormFields { get; set; } = new();
    public PdfMetadata? Metadata { get; set; }
    public string? ErrorMessage { get; set; }

    public void OnGet()
    {
        // Check if there's a file path from a previous upload in query string
        var path = Request.Query["path"].FirstOrDefault();
        if (!string.IsNullOrEmpty(path) && System.IO.File.Exists(path))
        {
            LoadPdfFields(path);
        }
    }

    public IActionResult OnPostUpload(IFormFile? pdfFile)
    {
        if (pdfFile == null || pdfFile.Length == 0)
        {
            ErrorMessage = "Please select a PDF file to upload.";
            return Page();
        }

        if (!pdfFile.FileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
        {
            ErrorMessage = "Only PDF files are accepted.";
            return Page();
        }

        try
        {
            // Save to uploads directory with a unique name
            var uploadsDir = Path.Combine(_env.WebRootPath, "uploads");
            Directory.CreateDirectory(uploadsDir);
            var uniqueName = $"{Guid.NewGuid():N}_{pdfFile.FileName}";
            var filePath = Path.Combine(uploadsDir, uniqueName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                pdfFile.CopyTo(stream);
            }

            LoadPdfFields(filePath);
            UploadedFileName = pdfFile.FileName;
        }
        catch (Exception ex)
        {
            if (ex.Message.Contains("encryption", StringComparison.OrdinalIgnoreCase) ||
                ex.Message.Contains("password", StringComparison.OrdinalIgnoreCase))
            {
                ErrorMessage = "This PDF uses encryption that is not supported. Please use an unencrypted or password-free PDF.";
            }
            else
            {
                ErrorMessage = $"Error processing PDF: {ex.Message}";
            }
        }

        return Page();
    }

    public IActionResult OnPostDownload()
    {
        var pdfPath = Request.Form["pdfPath"].FirstOrDefault();
        if (string.IsNullOrEmpty(pdfPath) || !System.IO.File.Exists(pdfPath))
        {
            ErrorMessage = "The uploaded PDF could not be found. Please upload again.";
            return Page();
        }

        try
        {
            // Collect field values from the form submission
            var fieldValues = new Dictionary<string, string>();
            foreach (var key in Request.Form.Keys)
            {
                if (key.StartsWith("field_"))
                {
                    var fieldName = key.Substring("field_".Length);
                    var value = Request.Form[key].LastOrDefault() ?? string.Empty;
                    fieldValues[fieldName] = value;
                }
            }

            var flatten = Request.Form["flatten"].FirstOrDefault() == "true";
            var filledPdf = _pdfService.FillForm(pdfPath, fieldValues, flatten);

            // Derive a nice download filename
            var originalName = Path.GetFileName(pdfPath);
            // Strip the GUID prefix we added during upload
            var underscoreIndex = originalName.IndexOf('_');
            if (underscoreIndex > 0)
                originalName = originalName.Substring(underscoreIndex + 1);

            var downloadName = Path.GetFileNameWithoutExtension(originalName) + "_filled.pdf";

            return File(filledPdf, "application/pdf", downloadName);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error filling PDF: {ex.Message}";
            // Re-load fields so the user can try again
            LoadPdfFields(pdfPath);
            return Page();
        }
    }

    private void LoadPdfFields(string filePath)
    {
        // Read fields first — if this throws (e.g., unsupported encryption),
        // HasUploadedPdf stays false and the user sees the upload screen with an error.
        var fields = _pdfService.GetFormFields(filePath);

        HasUploadedPdf = true;
        UploadedFilePath = filePath;
        UploadedFileName = Path.GetFileName(filePath);
        FormFields = fields;

        // Extract PDF metadata for the metadata card
        Metadata = _pdfService.GetMetadata(filePath, fields.Count);

        // Strip the GUID prefix from the display filename
        var displayName = UploadedFileName;
        var underscoreIndex = displayName.IndexOf('_');
        if (underscoreIndex > 0)
            displayName = displayName.Substring(underscoreIndex + 1);
        Metadata.FileName = displayName;
    }
}
