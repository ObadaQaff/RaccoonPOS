using SkiaSharp;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace RaccoonWarehouse.Helpers.Pdf
{
    internal static class Code39BarcodeRenderer
    {
        private static readonly IReadOnlyDictionary<char, string> Encodings = new Dictionary<char, string>
        {
            ['0']="101001101101",['1']="110100101011",['2']="101100101011",['3']="110110010101",['4']="101001101011",['5']="110100110101",['6']="101100110101",['7']="101001011011",['8']="110100101101",['9']="101100101101",
            ['A']="110101001011",['B']="101101001011",['C']="110110100101",['D']="101011001011",['E']="110101100101",['F']="101101100101",['G']="101010011011",['H']="110101001101",['I']="101101001101",['J']="101011001101",
            ['K']="110101010011",['L']="101101010011",['M']="110110101001",['N']="101011010011",['O']="110101101001",['P']="101101101001",['Q']="101010110011",['R']="110101011001",['S']="101101011001",['T']="101011011001",
            ['U']="110010101011",['V']="100110101011",['W']="110011010101",['X']="100101101011",['Y']="110010110101",['Z']="100110110101",['-']="100101011011",['.']="110010101101",[' ']="100110101101",
            ['$']="100100100101",['/']="100100101001",['+']="100101001001",['%']="101001001001",['*']="100101101101"
        };

        public static bool TryCreate(string? value, out string normalized, out string pattern)
        {
            normalized = value?.Trim().ToUpperInvariant() ?? string.Empty;
            pattern = string.Empty;
            if (normalized.Length == 0 || normalized.Any(c => c == '*' || !Encodings.ContainsKey(c))) return false;
            pattern = Encodings['*'] + "0" + string.Concat(normalized.Select(c => Encodings[c] + "0")) + Encodings['*'];
            return true;
        }

        public static ImageSource? CreateWpfImage(string? value)
        {
            if (!TryCreate(value, out _, out var pattern)) return null;
            const int moduleWidth = 2, quietZone = 10, height = 38;
            var width = quietZone * 2 + pattern.Length * moduleWidth;
            using var bitmap = new SKBitmap(width, height);
            using (var canvas = new SKCanvas(bitmap))
            using (var paint = new SKPaint { Color = SKColors.White, Style = SKPaintStyle.Fill })
            {
                canvas.DrawRect(0, 0, width, height, paint);
                paint.Color = SKColors.Black;
                for (var i = 0; i < pattern.Length; i++) if (pattern[i] == '1') canvas.DrawRect(quietZone + i * moduleWidth, 0, moduleWidth, height, paint);
            }
            using var image = SKImage.FromBitmap(bitmap);
            using var data = image.Encode(SKEncodedImageFormat.Png, 100);
            using var stream = new System.IO.MemoryStream(data.ToArray());
            var source = new BitmapImage();
            source.BeginInit(); source.CacheOption = BitmapCacheOption.OnLoad; source.StreamSource = stream; source.EndInit(); source.Freeze();
            return source;
        }

        public static byte[]? CreatePng(string? value)
        {
            if (!TryCreate(value, out _, out var pattern)) return null;
            const int moduleWidth = 2, quietZone = 10, height = 44;
            var width = quietZone * 2 + pattern.Length * moduleWidth;
            using var bitmap = new SKBitmap(width, height);
            using (var canvas = new SKCanvas(bitmap))
            using (var paint = new SKPaint { Color = SKColors.White, Style = SKPaintStyle.Fill })
            {
                canvas.DrawRect(0, 0, width, height, paint); paint.Color = SKColors.Black;
                for (var i = 0; i < pattern.Length; i++) if (pattern[i] == '1') canvas.DrawRect(quietZone + i * moduleWidth, 0, moduleWidth, height, paint);
            }
            using var image = SKImage.FromBitmap(bitmap); using var data = image.Encode(SKEncodedImageFormat.Png, 100); return data.ToArray();
        }
    }
}
