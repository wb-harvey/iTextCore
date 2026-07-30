Add-Type -Path "C:\AI\iTextCore\resources\iTextSharp-LGPL-Core\src\core\bin\Debug\netstandard2.0\iTextCore.API.dll"
[iTextCore.text.pdf.PdfReader]::AllowOpenWithoutOwnerPassword = $true

$pdfs = Get-ChildItem -Path "C:\Users\user\Documents\PDF-analysis" -Recurse -Filter *.pdf
$results = @()

foreach ($pdf in $pdfs) {
    try {
        $reader = New-Object iTextCore.text.pdf.PdfReader($pdf.FullName)
        
        $filename = $pdf.BaseName
        $fileSize = $pdf.Length
        $version = $reader.PdfVersion
        $encrypted = if ($reader.IsEncrypted()) { "Yes" } else { "No" }
        $formFields = if ($null -ne $reader.AcroFields -and $null -ne $reader.AcroFields.Fields) { $reader.AcroFields.Fields.Count } else { 0 }
        
        $info = $reader.Info
        $producer = if ($info.ContainsKey("Producer")) { $info["Producer"] } else { "" }
        $creator = if ($info.ContainsKey("Creator")) { $info["Creator"] } else { "" }
        
        $results += [PSCustomObject]@{
            Filename = $filename
            FileSize = $fileSize
            Version = $version
            Encrypted = $encrypted
            "Form Fields" = $formFields
            Producer = $producer
            Creator = $creator
        }
        
        $reader.Close()
    } catch {
        Write-Host "Failed to process $($pdf.FullName): $_"
        $results += [PSCustomObject]@{
            Filename = $pdf.BaseName
            FileSize = $pdf.Length
            Version = ""
            Encrypted = ""
            "Form Fields" = ""
            Producer = ""
            Creator = ""
        }
    }
}

$results | Export-Csv -Path "C:\Users\user\Documents\PDF-analysis\pdf-analysis-raw-results.csv" -NoTypeInformation -Encoding UTF8
Write-Host "Finished writing CSV"
