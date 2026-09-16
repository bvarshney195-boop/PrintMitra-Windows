# PrintMitra Windows MVP

PrintMitra is a fully offline Windows desktop application for simple and precise printing. This MVP focuses on photographed Aadhaar/PAN cards, exact physical sizing, native Windows printer selection and Canon driver properties.

## Implemented

- Windows 10/11 WPF application
- Easy and Operator modes
- Installed-printer discovery
- Native manufacturer Printer Properties
- Standard Windows print dialog
- A4, A5, B5, Letter, Legal and common photo-paper definitions
- Front/back identity-card layout at 85.60 × 53.98 mm
- OpenCV four-corner detection and perspective correction
- Conservative local contrast improvement
- One or multiple front/back sets with cutting guides
- Multi-image photo sheets with fit/fill layout
- Photographed/scanned document pages with multi-page preview
- PDF handoff to the installed Windows PDF application
- Passport/ID photo sheets with 35 × 45 mm, 2 × 2 inch and custom sizes
- Windows WIA scanner integration
- In-session Print Again workflow without retaining documents after exit
- Per-printer X/Y calibration profiles
- Automatic private temporary-workspace deletion
- No accounts, telemetry, cloud API or internet requirement at runtime

## Important MVP boundaries

- If four-corner detection fails, the processor uses a centred card-ratio crop. A production release should add draggable manual corner handles before printing.
- PDFs are opened in the installed Windows PDF application because Windows does not provide a stable built-in WPF PDF renderer. Image-based document pages preview and print inside PrintMitra.
- Passport cropping currently uses a centred fill crop. Authority-specific biometric face-position validation requires a separately validated face model and is not claimed in this release.
- Exact physical output depends on the printer driver. In Printer Properties keep scaling at **Actual Size / 100%**, then run calibration.
- Printed Aadhaar/PAN output is a copy of user-provided content; the app does not alter or recreate identity details or security features.

## Build on Windows

Requirements:

- Windows 10 or Windows 11 x64
- Visual Studio 2022 with **.NET desktop development**, or .NET 8 SDK

```powershell
dotnet restore .\PrintMitra.sln
dotnet build .\PrintMitra.sln -c Release
dotnet run --project .\src\PrintMitra\PrintMitra.csproj
```

## Create a self-contained Windows package

```powershell
.\scripts\publish-windows.ps1
```

Output: `artifacts\publish\win-x64\`

## Privacy

Images are processed locally. Corrected images are stored in a random temporary folder for the active session and removed when the app closes. Print history does not store card images or identity numbers.
