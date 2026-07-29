namespace PdfSharp.Models;

/// <summary>
/// Holds metadata extracted from a PDF document for display in the UI.
/// </summary>
public class PdfMetadata
{
    public string FileName { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public string PdfVersion { get; set; } = string.Empty;
    public int PageCount { get; set; }
    public bool HasForm { get; set; }
    public int FormFieldCount { get; set; }
    public bool IsEncrypted { get; set; }
    public string? Producer { get; set; }
    public string? Creator { get; set; }
    public string? PageSize { get; set; }
    public string? CreationDate { get; set; }
    public string? ModDate { get; set; }

    /// <summary>
    /// Returns the file size formatted as a human-readable string (e.g. "348 KB").
    /// </summary>
    public string FileSizeFormatted
    {
        get
        {
            if (FileSizeBytes < 1024)
                return $"{FileSizeBytes} B";
            if (FileSizeBytes < 1024 * 1024)
                return $"{FileSizeBytes / 1024.0:F1} KB";
            return $"{FileSizeBytes / (1024.0 * 1024.0):F1} MB";
        }
    }
}

