# Swatch Picker Port - Validation Notes

## Source basis

The conversion below is taken from `upstream/desktop/Binary/Tools/Swatcher.cs`, method `RGBtoHSV`. The Android page replaces the Windows Forms trackbars, color dialog, preview drawing, and clipboard calls. The numeric conversion remains in the platform-neutral `Binarius.Core` project.

## Formula preserved

1. Normalize each RGB channel to a float in `[0, 1]`.
2. Calculate `max`, `min`, `brightness = max`, and `saturation = max == 0 ? 0 : (max - min) / max`.
3. Calculate hue using the same red/green/blue maximum branches as the desktop source.
4. Convert the hue to the game's swatch scale using `paintSwatch = 90 - hue / 4`.
5. Cast the swatch value to `int`, matching the desktop implementation.

## Reference vectors

These vectors follow directly from the source formula and should be used as desktop-vs-Android regression checks:

| RGB | Paint swatch | Saturation | Brightness |
|---|---:|---:|---:|
| `0, 0, 0` | 90 | 0 | 0 |
| `255, 255, 255` | 90 | 0 | 1 |
| `255, 0, 0` | 90 | 1 | 1 |
| `0, 255, 0` | 60 | 1 | 1 |
| `0, 0, 255` | 30 | 1 | 1 |
| `255, 255, 0` | 75 | 1 | 1 |
| `0, 255, 255` | 45 | 1 | 1 |
| `255, 0, 255` | 15 | 1 | 1 |

## Validation status

- Source algorithm ported into `Binarius.Core/Services/PaintSwatchConverter.cs`.
- Android sliders, preview, output values, and clipboard actions implemented in `Binarius.Android/SwatchPickerPage.cs`.
- The values above are source-derived reference vectors. The C# project has not been compiled or run in this environment because the .NET SDK and Android workload are unavailable.
- Before marking this feature complete, compare all vectors with the desktop application and test slider interaction and clipboard behavior on an ARM64 Android device.
