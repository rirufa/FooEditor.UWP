using System.Collections.Generic;
using System.Globalization;
using Microsoft.Graphics.Canvas.Text;
namespace FooEditor.UWP.Models
{
    static class FontFamillyCollection
    {
        public static IEnumerable<string> GetFonts()
        {
            var fontCollection = CanvasTextFormat.GetSystemFontFamilies();
            var familyCount = fontCollection.Length;

            for (int i = 0; i < familyCount; i++)
            {
                yield return fontCollection[i];
            }
        }
    }
}
