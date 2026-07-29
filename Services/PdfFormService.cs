using System.Collections;
using iTextCore.text.pdf;
using PdfSharp.Models;

namespace PdfSharp.Services;

/// <summary>
/// Service that wraps iTextSharp operations for reading and populating PDF form fields.
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
    /// Fills form fields in the PDF with the provided values and returns the resulting PDF bytes.
    /// Optionally flattens the form (making fields non-editable).
    /// </summary>
    public byte[] FillForm(string pdfPath, Dictionary<string, string> fieldValues, bool flatten = false)
    {
        PdfReader? reader = null;
        PdfStamper? stamper = null;
        var outputStream = new MemoryStream();

        try
        {
            reader = new PdfReader(pdfPath);
            stamper = new PdfStamper(reader, outputStream);

            var acroFields = stamper.AcroFields;

            foreach (var kvp in fieldValues)
            {
                acroFields.SetField(kvp.Key, kvp.Value);
            }

            stamper.FormFlattening = flatten;
        }
        finally
        {
            stamper?.Close();
            reader?.Close();
        }

        return outputStream.ToArray();
    }

    /// <summary>
    /// Maps iTextSharp's integer field type constants to our FormFieldType enum.
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
