using System.Collections;
using iTextCore.text.pdf;
using iTextCore.Models;

namespace iTextCore.Services;

/// <summary>
/// Service that wraps iTextCore operations for reading and populating PDF form fields.
/// Uses PdfReader to extract AcroFields metadata and PdfStamper to write updated values.
/// </summary>
public class PdfFormService
{
    /// <summary>
    /// Reads all form fields from the specified PDF file.
    /// Returns metadata including field name, type, current value, and options (for combo/list/radio).
    /// </summary>
    public List<FormFieldInfo> GetFormFields(string pdfPath)
    {
        var fields = new List<FormFieldInfo>();
        PdfReader? reader = null;

        try
        {
            reader = new PdfReader(pdfPath);
            var acroFields = reader.AcroFields;

            if (acroFields?.Fields == null)
                return fields;

            foreach (DictionaryEntry fieldEntry in acroFields.Fields)
            {
                var fieldName = fieldEntry.Key?.ToString();
                if (string.IsNullOrEmpty(fieldName))
                    continue;

                var fieldType = acroFields.GetFieldType(fieldName);
                var fieldValue = acroFields.GetField(fieldName) ?? string.Empty;

                var field = new FormFieldInfo
                {
                    Name = fieldName,
                    CurrentValue = fieldValue,
                    FieldType = MapFieldType(fieldType),
                    Options = new List<string>()
                };

                // Extract options for combo boxes, list boxes, and radio buttons
                if (field.FieldType == FormFieldType.ComboBox ||
                    field.FieldType == FormFieldType.ListBox ||
                    field.FieldType == FormFieldType.RadioButton)
                {
                    var appearances = acroFields.GetAppearanceStates(fieldName);
                    if (appearances != null)
                    {
                        field.Options = appearances
                            .Where(a => a != "Off")
                            .ToList();
                    }
                }

                // Skip signature and push button fields — they're not user-editable via text
                if (field.FieldType == FormFieldType.Signature ||
                    field.FieldType == FormFieldType.PushButton)
                    continue;

                fields.Add(field);
            }
        }
        finally
        {
            reader?.Close();
        }

        return fields;
    }

    /// <summary>
    /// Extracts metadata from the PDF file for display in the UI.
    /// </summary>
    public PdfMetadata GetMetadata(string pdfPath, int formFieldCount)
    {
        var fileInfo = new FileInfo(pdfPath);
        var metadata = new PdfMetadata
        {
            FileName = fileInfo.Name,
            FileSizeBytes = fileInfo.Length,
            FormFieldCount = formFieldCount
        };

        PdfReader? reader = null;
        try
        {
            reader = new PdfReader(pdfPath);

            // PdfVersion returns just the minor version char (e.g. '6'), so prepend "1."
            metadata.PdfVersion = $"1.{reader.PdfVersion}";
            metadata.PageCount = reader.NumberOfPages;
            metadata.IsEncrypted = reader.IsEncrypted();
            metadata.HasForm = reader.AcroFields?.Fields?.Count > 0;

            // Extract page size from the first page (in PDF points: 72 points = 1 inch)
            if (reader.NumberOfPages > 0)
            {
                var pageSize = reader.GetPageSizeWithRotation(1);
                var widthIn = pageSize.Width / 72.0;
                var heightIn = pageSize.Height / 72.0;
                metadata.PageSize = $"{widthIn:F1}\" × {heightIn:F1}\"";

                // Detect common page sizes
                if (IsApprox(widthIn, 8.5) && IsApprox(heightIn, 11))
                    metadata.PageSize += " (Letter)";
                else if (IsApprox(widthIn, 8.5) && IsApprox(heightIn, 14))
                    metadata.PageSize += " (Legal)";
                else if (IsApprox(widthIn, 8.27) && IsApprox(heightIn, 11.69))
                    metadata.PageSize += " (A4)";
            }

            var info = reader.Info;
            if (info != null)
            {
                metadata.Producer = info["Producer"] as string;
                metadata.Creator = info["Creator"] as string;
                metadata.CreationDate = FormatPdfDate(info["CreationDate"] as string);
                metadata.ModDate = FormatPdfDate(info["ModDate"] as string);
            }
        }
        finally
        {
            reader?.Close();
        }

        return metadata;
    }

    /// <summary>
    /// Checks if two double values are approximately equal (within 0.15).
    /// </summary>
    private static bool IsApprox(double a, double b) => Math.Abs(a - b) < 0.15;

    /// <summary>
    /// Converts a PDF date string (e.g. "D:20250306133302-08'00'") to a human-readable format.
    /// </summary>
    private static string? FormatPdfDate(string? pdfDate)
    {
        if (string.IsNullOrEmpty(pdfDate))
            return null;

        // Strip the "D:" prefix
        var s = pdfDate.StartsWith("D:") ? pdfDate.Substring(2) : pdfDate;

        // Try to parse: YYYYMMDDHHmmSS followed by optional timezone
        if (s.Length >= 14)
        {
            try
            {
                var year = s.Substring(0, 4);
                var month = s.Substring(4, 2);
                var day = s.Substring(6, 2);
                return $"{year}-{month}-{day}";
            }
            catch
            {
                // Fall through to return raw string
            }
        }

        return pdfDate;
    }

    /// <summary>
    /// Fills form fields in the PDF with the provided values and returns the resulting PDF bytes.
    /// By default, the output PDF has no owner password, no encryption, and no certificates.
    /// Set the keep* parameters to true to preserve the original security settings.
    /// </summary>
    public byte[] FillForm(
        string pdfPath,
        Dictionary<string, string> fieldValues,
        bool flatten = false,
        bool keepSignatures = false,
        bool keepOwnerPassword = false,
        bool keepEncryption = false,
        bool keepCertificates = false)
    {
        PdfReader? reader = null;
        PdfStamper? stamper = null;
        var outputStream = new MemoryStream();

        try
        {
            // Enable bypass so PdfStamper doesn't throw on encrypted PDFs
            // even when the owner password is unknown
            PdfReader.AllowOpenWithoutOwnerPassword = true;

            reader = new PdfReader(pdfPath);
            
            // Remove Adobe Reader Extended Features (Usage Rights) which restrict document changes.
            reader.RemoveUsageRights();

            if (keepEncryption || keepOwnerPassword || keepCertificates)
            {
                // Append mode preserves the original encryption, owner password,
                // and certificate settings from the source PDF
                stamper = new PdfStamper(reader, outputStream, '\0', true);
            }
            else
            {
                // Standard mode rewrites the PDF without encryption,
                // producing a clean output with no security restrictions
                stamper = new PdfStamper(reader, outputStream);
            }

            // Remove XFA from the PDF to prevent Adobe Acrobat from throwing type validation 
            // errors (e.g. "Invalid value 'Off' specified for element...") during form synchronization.
            var acroFormDict = reader.Catalog?.GetAsDict(PdfName.ACROFORM);
            acroFormDict?.Remove(PdfName.XFA);

            var acroFields = stamper.AcroFields;
            
            if (acroFields != null)
            {
                if (!keepSignatures)
                {
                    // Remove any actual signature fields
                    var sigNames = acroFields.GetSignatureNames();
                    foreach (string name in sigNames)
                    {
                        acroFields.RemoveField(name);
                    }
                    
                    // Remove SigFlags from AcroForm so viewers don't expect a signature
                    acroFormDict?.Remove(PdfName.SIGFLAGS);
                }

                foreach (var kvp in fieldValues)
                {
                    acroFields.SetField(kvp.Key, kvp.Value);
                }
            }

            stamper.FormFlattening = flatten;
        }
        finally
        {
            stamper?.Close();
            reader?.Close();
            PdfReader.AllowOpenWithoutOwnerPassword = false;
        }

        return outputStream.ToArray();
    }

    /// <summary>
    /// Maps iTextCore's integer field type constants to our FormFieldType enum.
    /// </summary>
    private static FormFieldType MapFieldType(int iTextFieldType)
    {
        return iTextFieldType switch
        {
            AcroFields.FIELD_TYPE_TEXT => FormFieldType.Text,
            AcroFields.FIELD_TYPE_CHECKBOX => FormFieldType.Checkbox,
            AcroFields.FIELD_TYPE_RADIOBUTTON => FormFieldType.RadioButton,
            AcroFields.FIELD_TYPE_PUSHBUTTON => FormFieldType.PushButton,
            AcroFields.FIELD_TYPE_LIST => FormFieldType.ListBox,
            AcroFields.FIELD_TYPE_COMBO => FormFieldType.ComboBox,
            AcroFields.FIELD_TYPE_SIGNATURE => FormFieldType.Signature,
            _ => FormFieldType.Unknown
        };
    }
}
