using System.Collections.Generic;
using System.Globalization;
using SharpDX.DirectWrite;
namespace FooEditor.UWP.Models
{
    static class FontFamillyCollection
    {
        public static IEnumerable<string> GetFonts()
        {
            var factory = new Factory();
            var fontCollection = factory.GetSystemFontCollection(new SharpDX.Mathematics.Interop.RawBool(false));
            var familyCount = fontCollection.FontFamilyCount;

            for (int i = 0; i < familyCount; i++)
            {
                var fontFamily = fontCollection.GetFontFamily(i);
                var familyNames = fontFamily.FamilyNames;
                int index;

                if (!familyNames.FindLocaleName(CultureInfo.CurrentCulture.Name, out index))
                    familyNames.FindLocaleName("en-us", out index);

                yield return familyNames.GetString(index);
            }
        }
    }
}
