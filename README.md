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
- Per-printer X/Y calibration profiles
- Automatic private temporary-workspace deletion
- No accounts, telemetry, cloud API or internet requirement at runtime

## Important MVP boundaries

- If four-corner detection fails, the processor uses a centred card-ratio crop. A production release should add draggable manual corner handles before printing.
- PDF rendering, scanning/WIA, passport-photo presets and general photo grids are planned modules, not yet implemented in this MVP.
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
