using Binarius.Core.Models;

namespace Binarius.Core.Services;

/// <summary>
/// Port of the RGB-to-paint-swatch calculation in the supplied desktop
/// Binary.Tools.Swatcher source. Values are intentionally kept as floats to
/// preserve the original calculation's behavior.
/// </summary>
public static class PaintSwatchConverter
{
    public static PaintSwatchResult Convert(byte redByte, byte greenByte, byte blueByte)
    {
        var red = redByte / 255f;
        var green = greenByte / 255f;
        var blue = blueByte / 255f;

        var max = Math.Max(red, Math.Max(green, blue));
        var min = Math.Min(red, Math.Min(green, blue));
        var brightness = max;
        var difference = max - min;
        var saturation = max == 0 ? 0 : difference / max;

        float hue;
        if (max == min)
        {
            hue = 0;
        }
        else if (max == red)
        {
            hue = ((60 * ((green - blue) / difference)) + 360) % 360;
        }
        else if (max == green)
        {
            hue = ((60 * ((blue - red) / difference)) + 120) % 360;
        }
        else
        {
            hue = ((60 * ((red - green) / difference)) + 240) % 360;
        }

        // The desktop tool maps hue degrees to the game's paint-swatch scale.
        hue = 90 - (hue / 4);

        return new PaintSwatchResult((int)hue, saturation, brightness);
    }
}
