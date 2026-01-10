using System;
using System.Collections.Generic;
using System.Windows.Media;

namespace HexaFlow.Utils
{
    public static class FontHelper
    {
        public static List<string> GetSystemFonts()
        {
            var fonts = new List<string>();
            foreach (FontFamily fontFamily in Fonts.SystemFontFamilies)
            {
                fonts.Add(fontFamily.Source);
            }
            return fonts;
        }
        
        public static bool FontExists(string fontName)
        {
            foreach (FontFamily fontFamily in Fonts.SystemFontFamilies)
            {
                if (fontFamily.Source.Equals(fontName, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }
    }
}