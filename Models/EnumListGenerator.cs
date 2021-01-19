using System;
using System.Collections.Generic;

namespace FooEditor.UWP.Models
{
    class EnumListGenerator
    {
        public static IEnumerable<T> GetList<T>()
        {
            foreach (T enum_value in Enum.GetValues(typeof(T)))
                yield return enum_value;
        }
    }
}
