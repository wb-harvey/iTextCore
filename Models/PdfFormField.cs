namespace PdfSharp.Models;

/// <summary>
/// Represents a single form field extracted from a PDF document.
/// Named FormFieldInfo to avoid collision with iTextCore.text.pdf.PdfFormField.
/// </summary>
public class FormFieldInfo
{
    public string Name { get; set; } = string.Empty;
    public string CurrentValue { get; set; } = string.Empty;
    public FormFieldType FieldType { get; set; }
    public List<string> Options { get; set; } = new();
}

/// <summary>
/// Enumerates the supported PDF form field types.
/// Maps to iTextCore AcroFields field type constants.
/// </summary>
public enum FormFieldType
{
    Text = 1,
    Checkbox = 2,
    RadioButton = 3,
    ComboBox = 6,
    ListBox = 5,
    PushButton = 4,
    Signature = 7,
    Unknown = 0
}

