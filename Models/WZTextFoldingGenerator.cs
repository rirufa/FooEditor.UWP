using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FooEditEngine;
using FooEditor;

namespace FooEditor.UWP.Models
{
    sealed class WZTextFoldingGenerator : IFoldingStrategy
    {
        struct TextLevelInfo
        {
            public int Index;
            public int Level;
            public TextLevelInfo(int index, int level)
            {
                this.Index = index;
                this.Level = level;
            }
        }
        public IEnumerable<FoldingItem> AnalyzeDocument(Document doc, int start, int end)
        {
            Stack<TextLevelInfo> beginIndexs = new Stack<TextLevelInfo>();
            int lineHeadIndex = start;
            foreach (string lineStr in doc.GetLines(start, end))
            {
                int level = OutlineAnalyzer.GetWZTextLevel(lineStr);
                if (level != -1)
                {
                    foreach(FoldingItem item in GetFoldings(beginIndexs,level, lineHeadIndex))
                        yield return item;
                    beginIndexs.Push(new TextLevelInfo(lineHeadIndex, level));
                }
                lineHeadIndex += lineStr.Length;
            }
            foreach (FoldingItem item in GetFoldings(beginIndexs, 0, lineHeadIndex))
                yield return item;
        }

        IEnumerable<FoldingItem> GetFoldings(Stack<TextLevelInfo> beginIndexs,int level,int lineHeadIndex)
        {
            while (beginIndexs.Count > 0)
            {
                TextLevelInfo begin = beginIndexs.Peek();
                if (level > begin.Level)
                    break;
                beginIndexs.Pop();
                int endIndex = lineHeadIndex - 1;
                if (begin.Index < endIndex)
                    yield return new OutlineItem(begin.Index, endIndex,begin.Level);
            }
        }
    }
}
