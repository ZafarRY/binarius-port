# Binarius Android Porting Analysis

## 1. Source examined

The analysis is based on the uploaded `Binarius.zip`, specifically:

- `Binary/Binary.csproj`
- `Binary.sln`
- `Readme.md`
- C# source files under `Binary/`
- data files under `Binary/Data/`

The supplied ZIP contains the desktop application source and data, but **does not contain** the four `Modules/*` submodule working trees. The `.gitmodules` file was supplied separately in chat and is reproduced in this port repository.

## 2. Verified desktop architecture

The uploaded project is a Windows Forms application:

- Target framework: `net6.0-windows`
- `UseWindowsForms`: `true`
- Output type: `WinExe`
- Package reference: `System.Drawing.Common` 5.0.3
- Project references: CoreExtensions, Endscript, ILWrapper, and Nikki/Darius
- Desktop application version in the project: 2.10.4

The source snapshot contains approximately 16,751 lines across 73 C# files, including 35 Windows Forms designer files. A source scan found extensive `System.Windows.Forms` and `System.Drawing` usage. These are not portable UI dependencies and cannot be carried into a MAUI Android UI unchanged.

No `DllImport` or `LibraryImport` declarations were found in the supplied `Binary/` application C# files. That does **not** prove the dependency tree is free of native code; `ILWrapper` and its transitive dependencies still need inspection.

## 3. Feature inventory from the source and upstream README

The upstream README describes collection/database editing, commands, backup and restore, import/export, Endscript execution, NFS Hasher, NFS Raider, Swatch Picker, Texture Editor, String Editor, CarPart Editor, and Career Editor.

The supplied application source also contains Vector Editor/Display and LAN Maker-related screens. These have been included in the port inventory so they are not silently omitted.

## 4. Porting consequences

| Desktop dependency or pattern | Android port decision |
|---|---|
| Windows Forms forms, controls, TreeView, PropertyGrid, dialogs | Rebuild screens and interaction using MAUI controls and MVVM/view models |
| `System.Drawing.Common` | Replace with Android-compatible image pipeline; do not assume the Windows GDI+ implementation works on Android |
| Windows file dialogs and path assumptions | Use Android Storage Access Framework via MAUI `FilePicker` and an explicit export/save implementation |
| Clipboard APIs | Use MAUI clipboard abstraction or Android platform service |
| Windows process launching and shell integration | Replace with MAUI launcher/share APIs or Android-specific services |
| `Properties.Settings` and Windows configuration | Replace with MAUI Preferences/SecureStorage and app-local settings |
| `SpeedReflect.asi` | Windows/ASI-specific component; excluded from Android assets until a compatible replacement exists |
| Endscript, Darius/Nikki, CoreExtensions | Audit source and target frameworks; port/re-target only the portions that are platform-neutral |
| ILWrapper and texture codec dependencies | Inspect native binaries and P/Invoke. Replace or rebuild codecs for Android `arm64-v8a` |

## 5. Initial implementation in this repository

Implemented in the initial scaffold:

- .NET MAUI Android project targeting `net10.0-android`.
- ARM64 CI publish configuration (`android-arm64`).
- Cross-platform `Binarius.Core` project.
- Android file selection, copying the selected stream into app-private workspace storage, and non-mutating inspection: extension detection, file length, and a 64-byte hex/ASCII preview. The original source file is not modified.
- Feature inventory screen. Editor features remain explicitly marked as not yet ported, except Swatch Picker which has an initial MAUI UI and a direct port of the RGB-to-paint-swatch calculation.
- Original desktop source snapshot and static game key/theme data retained for reference.
- `SpeedReflect.asi` is not packaged.
- GitHub Actions workflow for running core smoke tests and building/uploading a Debug APK artifact. Submodule checkout remains disabled until gitlink records are committed.

Not implemented yet:

- Parsing or writing BIN, BUN, or LZC formats.
- Device-level validation of the Swatch Picker port against desktop reference values (smoke-test cases are configured for CI).
- Database/collection editor and property editing.
- Endscript execution.
- Texture decoding/encoding and replacement.
- String, vector, car-parts, and career editing.
- Import/export, backups, restore, and LAN Maker workflows.
- Release signing and device-level testing.

## 6. Required next source input

Before wiring the original engine into `Binarius.Core`, inspect the exact source of:

1. `Modules/Endscript`
2. `Modules/ILWrapper`
3. `Modules/Darius`
4. `Modules/CoreExtensions`

The user has supplied their GitHub URLs. The uploaded ZIP does not contain their source. After restoring the modules, inspect each module's own `.gitmodules` file and validate any relative paths before enabling recursive checkout. The source tree still needs to be reviewed before it is safe to reference from the Android project.

## 7. Validation gate

A feature is considered ported only after:

1. The same source file can be loaded on Android.
2. The app can parse it without changing the original bytes.
3. A controlled edit can be applied and saved to a separate output file.
4. Reopening the saved file yields the expected data.
5. A round-trip test compares the output with a known-good desktop result or a format-specific expected result.
6. The feature passes on a physical ARM64 Android device.

## Swatch Picker port notes

The RGB-to-paint-swatch calculation is separated into `Binarius.Core/Services/PaintSwatchConverter.cs`. The Android UI replacement is `Binarius.Android/SwatchPickerPage.cs`. Formula details and source-derived reference vectors are documented in `PORTING_SWATCH_VALIDATION.md`. This feature remains pending build and physical-device validation.
