using PdfSharpCore.Fonts;
using System;
using System.Collections.Generic;
using System.Linq;
using PdfSharpCore.Fonts;
using System.IO;



namespace RaccoonWarehouse.Helpers
{
    public class FontResolver : IFontResolver
    {
        private readonly string _fontPath;

        public FontResolver(string fontPath)
        {
            _fontPath = fontPath;
        }

        public string DefaultFontName => "CustomArabicFont";

        public byte[] GetFont(string faceName)
        {
            return File.ReadAllBytes(_fontPath);
        }

        public FontResolverInfo ResolveTypeface(string familyName, bool isBold, bool isItalic)
        {
            return new FontResolverInfo("CustomArabicFont");
        }
    }
}
