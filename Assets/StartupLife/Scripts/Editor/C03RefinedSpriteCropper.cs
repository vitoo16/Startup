using System;
using System.IO;
using UnityEngine;

namespace StartupLife.Editor
{
    // Editor-only conversion of a SHA-validated, full-canvas owner reference
    // cutout into a compact QA sprite. This cannot create hidden anatomy.
    // All RGBA texels inside the source alpha bounds are copied unchanged.
    // The returned center offset keeps the static assembled pose aligned.
    internal static class C03RefinedSpriteCropper
    {
        internal const int SourceWidth = 1122;
        internal const int SourceHeight = 1402;
        internal const int TransparentPaddingPixels = 2;

        internal static Vector2 CropToSprite(
            string absoluteSource, string assetDestination, float pixelsPerUnit)
        {
            if (pixelsPerUnit <= 0f || !File.Exists(absoluteSource))
                throw new ArgumentException("Invalid C03 QA source or pixels-per-unit.");
            if (File.Exists(assetDestination))
                throw new IOException("Never overwrite an existing C03 cropped QA layer.");

            Texture2D source = null;
            Texture2D trimmed = null;
            try
            {
                source = new Texture2D(2, 2, TextureFormat.RGBA32, false, true);
                if (!ImageConversion.LoadImage(source, File.ReadAllBytes(absoluteSource), false)
                    || source.width != SourceWidth || source.height != SourceHeight)
                    throw new InvalidDataException(
                        "Expected exactly 1122x1402 decoded RGBA owner-reference PNG.");

                var pixels = source.GetPixels32(); // Unity bottom-left origin.
                var left = SourceWidth;
                var right = -1;
                var bottom = SourceHeight;
                var top = -1;
                for (var y = 0; y < SourceHeight; y++)
                {
                    for (var x = 0; x < SourceWidth; x++)
                    {
                        if (pixels[y * SourceWidth + x].a == 0) continue;
                        if (x < left) left = x;
                        if (x > right) right = x;
                        if (y < bottom) bottom = y;
                        if (y > top) top = y;
                    }
                }

                if (right < left || top < bottom)
                    throw new InvalidDataException("C03 source semantic layer is fully transparent.");

                left = Mathf.Max(0, left - TransparentPaddingPixels);
                bottom = Mathf.Max(0, bottom - TransparentPaddingPixels);
                right = Mathf.Min(SourceWidth - 1, right + TransparentPaddingPixels);
                top = Mathf.Min(SourceHeight - 1, top + TransparentPaddingPixels);
                var width = right - left + 1;
                var height = top - bottom + 1;
                var cropPixels = new Color32[width * height];
                for (var y = 0; y < height; y++)
                    Array.Copy(pixels, (bottom + y) * SourceWidth + left,
                        cropPixels, y * width, width);

                trimmed = new Texture2D(width, height, TextureFormat.RGBA32, false, true);
                trimmed.SetPixels32(cropPixels);
                trimmed.Apply(false, false);
                var encoded = ImageConversion.EncodeToPNG(trimmed);
                if (encoded == null || encoded.Length == 0)
                    throw new InvalidDataException("Unity could not encode cropped C03 PNG.");
                File.WriteAllBytes(assetDestination, encoded);

                // The imported crop has SpriteAlignment.Center. Offset its
                // center from the original full-canvas center by the same
                // source pixels so it overlays the exact bind pose.
                return new Vector2(
                    (left + width * .5f - SourceWidth * .5f) / pixelsPerUnit,
                    (bottom + height * .5f - SourceHeight * .5f) / pixelsPerUnit);
            }
            finally
            {
                if (trimmed != null) UnityEngine.Object.DestroyImmediate(trimmed);
                if (source != null) UnityEngine.Object.DestroyImmediate(source);
            }
        }
    }
}
