# iTextCore 10.0.1 (LGPL / MPL)

iTextCore is an unofficial .NET port of the iTextSharp 4.1.6 library (originally a C# port of the open-source Java library for PDF generation).


## Supported Frameworks
The following frameworks are supported:
- .NET 10.0
- .NET Standard 2.0

## Installation
Build from source, and use the library for your own projects.

## Compatibility
There are many "modern" PDF features that were introduced in newer versions of iText, and you may want to use them with a paid license from the company that owns iText:

- [Corporate homepage of iText](http://itextpdf.com/)

## Licensing
This library is LGPL 2.1, based on the legacy iTextSharp 4.1.6 codebase, which was the last version of the library released under the Mozilla Public License and the LGPL before the project moved to the AGPL. 

This library includes dependencies and components with permissive licensing:
- **jCraft ZLIB**: 3-Clause BSD license.
- **BouncyCastle** (`Portable.BouncyCastle`): MIT License (Legion of the Bouncy Castle).

The unmodified source code and copyright notices for included components are preserved.

This version can be used as a free PDF library in your own closed-source projects.

## Updates and bug fixes

Code generation by Claude Opus 4.6
Code reviews by Gemini Pro 3.1

- [Change] Removed NuGet packaging
- [Change] Removed legacy .net (4.0, 4.5) frameworks
- [Fix] Fixed System.Drawing.Common (CVE-2021-24112)
- [Fix] Removed duplicate reference to System.Text.Encoding.CodePages
- [Fix] Suppressed obsolete BouncyCastle API warnings in third-party source
- [Add] Added support for AES-256 encrypted PDF files (R-5, R-6 encryptions)
 


