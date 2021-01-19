using System;
using System.Collections.Generic;
using System.Text;
using FooEditEngine;

namespace FooEditor
{
    sealed class AutoIndent
    {
        public string[] IndentStart, IndentEnd;

        /// <summary>
        /// オートインデントを行うなら真。そうでないなら偽
        /// </summary>
        public bool Enable
        {
            get;
            set;
        }

        public void InsertIndent(Document doc)
        {
            if (this.Enable == false)
                return;

            StringBuilder temp = new StringBuilder();

            TextPoint cur = doc.CaretPostion;

            string lineString = doc.LayoutLines[cur.row > 0 ? cur.row - 1 : 0];

            int tabNum = this.GetIntendLevel(lineString);

            if (hasWords(lineString, this.IndentEnd))
                tabNum--;
            else if (hasWords(lineString, this.IndentStart))
                tabNum++;

            if (tabNum < 0)
                tabNum = 0;

            for (int i = 0; i < tabNum; i++)
                temp.Append('\t');

            if (temp.Length > 0)
            {
                int index = doc.LayoutLines.GetIndexFromTextPoint(cur);
                doc.Insert(index, temp.ToString());
            }
        }

        bool hasWords(string s, IList<string> words)
        {
            if (words == null)
                return false;
            foreach (string word in words)
                if (s.IndexOf(word) != -1)
                    return true;
            return false;
        }

        int GetIntendLevel(string s)
        {
            int level = 0;
            for (int i = 0; i < s.Length; i++)
            {
                if (s[i] != '\t')
                    break;
                level++;
            }
            return level;
        }
    }
}
