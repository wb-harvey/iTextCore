# iTextCore 10.0.1 (LGPL / MPL)

iTextCore is an unofficial .NET Core port of the iTextSharp 4.1.6 library, which is a port of open source Java library for PDF generation written entirely in C# for the .NET platform.

It is LGPL, based on the legacy iTextSharp 4.1.6 codebase, which was the last version of the library released under the Mozilla Public License and the LGPL before the project moved to the AGPL. 

You can use this version if you need a free PDF library for use in closed-source projects.

## Supported Frameworks
The following frameworks are supported:
- .Net Core 10
- .Net Standard 2

## Installation
Build from source, and use the library for your own projects.

## Updates and bug fixes
There are many "modern" PDF features that were introduced in newer versions of iText, and you may want to use them with a paid license from the company that owns iText:

- [Corporate homepage of iText](http://itextpdf.com/)

This library, however, maintains LGPL licensing.

Code generation by Claude Opus 4.6
Code reviews by Gemini Pro 3.1

- [Change] Removed NuGet packaging
- [Change] Removed legacy .net (4.0, 4.5) frameworks
- [Fix] Fixed System.Drawing.Common (CVE-2021-24112)
- [Fix] Removed duplicate reference to System.Text.Encoding.CodePages
- [Fix] Suppressed obsolete BouncyCastle API warnings in third-party source
- [Add] Added support for AES-256 encrypted PDF files (R-5, R-6 encryptions)
 


