using System;
using System.IO;
using System.Text;
using FooEditEngine;
using FooEditEngine.UWP;

namespace FooEditor.UWP.Models
{
    static class OutlineActions
    {
        public static void JumpNode(OutlineTreeItem SelectedNode, Document doc)
        {
            if (SelectedNode == null)
                return;
            var tp = doc.LayoutLines.GetTextPointFromIndex(SelectedNode.Start);
            doc.CaretPostion = tp;
            doc.RequestRedraw();
        }

        public static string FitOutlineLevel(string str, OutlineTreeItem item, int childNodeLevel)
        {
            StringReader sr = new StringReader(str);
            StringBuilder text = new StringBuilder();

            string line = sr.ReadLine();
            int level = item.Level;
            int delta = 0;
            if (level > childNodeLevel)
                delta = -1;
            else if (level < childNodeLevel)
                delta = childNodeLevel - level;

            if (delta != 0)
            {
                text.Append(GetNewTitleText(line, level + delta) + "\n");
                while ((line = sr.ReadLine()) != null)
                {
                    level = OutlineAnalyzer.GetWZTextLevel(line);
                    if (level != -1)
                        text.Append(GetNewTitleText(line, level + delta) + "\n");
                    else
                        text.Append(line + "\n");
                }
            }

            sr.Dispose();
            
            return text.ToString();
        }

        static string GetNewTitleText(string line, int level)
        {
            if (level < 0)
                throw new ArgumentOutOfRangeException();
            StringBuilder output = new StringBuilder();
            for (int i = 0; i <= level; i++)
                output.Append('.');
            output.Append(line.TrimStart(new char[] { '.' }));
            return output.ToString();
        }
    }
}
