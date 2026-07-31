# UsefulPdfNet 10.0.1 (LGPL / MPL)

Copyright (c) 2026 William Harvey

UsefulPdfNet is an unofficial .NET port of the iTextSharp 4.1.6 library, originally a C# port of the open-source Java library for PDF generation.

## Supported Frameworks

The following frameworks are supported:
- .NET 10.0
- .NET Standard 2.0

## Installation

Build from source, and use the library for your own projects.

## Licensing

This library is LGPL 2.1, based on the legacy iTextSharp 4.1.6 codebase. This was the last version of the library released under the Mozilla Public License and the LGPL before the project moved to the AGPL.

This library includes dependencies and components with permissive licensing:
- **jCraft ZLIB**: 3-Clause BSD license.
- **BouncyCastle** (`Portable.BouncyCastle`): MIT License (Legion of the Bouncy Castle).

The unmodified source code and copyright notices for included components are preserved.

This version can be used as a free PDF library in your own closed-source projects.

There are many newer PDF features that were introduced in newer versions of iTextSharp and iTextCore.  If you need that you might want to get a paid license from iText:  

- [Corporate homepage of iText](http://itextpdf.com/)

## Updates and Bug Fixes

Code generation and contributions by Claude Opus 4.6.  
Code reviews by Gemini Pro 3.1.

- **[Add]** Added support for AES-256 encrypted PDF files (R-5, R-6 encryptions).
- **[Change]** Removed NuGet packaging.
- **[Change]** Removed legacy .NET (4.0, 4.5) frameworks.
- **[Change]** Strip XFA (XML Forms Architecture) contents for exported PDF files.
- **[Change]** Strip Usage Rights from exported PDF files (enabled editing of exported PDF files).
- **[Fix]** Fixed System.Drawing.Common (CVE-2021-24112).
- **[Fix]** Removed duplicate reference to System.Text.Encoding.CodePages.
- **[Fix]** Suppressed obsolete BouncyCastle API warnings in third-party source.
- **[Fix]** Improve flatten PDF by removing Helvetica font error.
- **[Fix]** Defensive code for browser error when download button disabled before download completes.
- **[Fix]** Add Text Padding Y option. Value -1.0 matches Adobe proprietary text rendering

## The Evolution of PDF Files and ISO Standards

PDF is dead.  
Long live PDF.  

PDF files are supposed to follow the ISO standard:
- [ISO 32000-1 (PDF 1.7) 2008](https://www.iso.org/standard/51502.html)
- [ISO 32000-2 (PDF 2.0)](https://www.iso.org/standard/75839.html)
- [ISO 32000-2:2017](https://www.iso.org/standard/63530.html)
- [ISO 32000-2:2020](https://www.iso.org/standard/75839.html)

XFA was a failed attempt by Adobe to force a proprietary forms structure into PDF files, but all modern browsers do not use it. Often, users will experience issues if they open a PDF fillable document in their browser, save it, and then find the form blank when they open it in Adobe Acrobat.

eSignatures are an ongoing issue. Many providers offer various levels of verifiable eSignature capabilities and stuff them into PDF documents. This adds layers of proprietary digital signing and modifications to PDF files to verify provenance. The vendors trying this start with Adobe and continue down the alphabet. It's a mess. 

This version of UsefulPdfNet does not support XFA or eSignatures.
