using FooEditEngine;

namespace FooEditor.UWP.Models
{
    class GenericCompleter
    {
        public static void LoadKeywords(AutoCompleteBoxBase CompleteBox, SyntaxDefnition syntax_def)
        {
            CompleteBox.Items.Clear();
            CompleteBox.Operators = syntax_def.Operators;
            CompleteBox.ShowingCompleteBox = (s, e) => { };

            foreach (string s in syntax_def.Keywords)
                CompleteBox.Items.Add(new CompleteWord(s));
            foreach (string s in syntax_def.Keywords2)
                CompleteBox.Items.Add(new CompleteWord(s));
        }
    }
}
