using FooEditEngine;
using System;
using System.Linq;
using System.Text;

namespace FooEditor.UWP.Models
{
    public class Snippet
    {
        public string Name
        {
            get;
            set;
        }
        public string Data
        {
            get;
            set;
        }
        public Snippet(string name, string data)
        {
            this.Name = name;
            this.Data = data;
        }
        public void InsetToDocument(DocumentModel docModel)
        {
            Document doc = docModel.Document;
            int lineNumber = doc.CaretPostion.row;
            string lineString = doc.LayoutLines[lineNumber];
            int tabNum = lineString.Count((c) => { return c == '\t'; });

            string insertText = this.GetParsedSnipped(this.Data, docModel.Encode, tabNum);

            int caretIndex = doc.LayoutLines.GetIndexFromTextPoint(docModel.Document.CaretPostion);
            doc.Insert(caretIndex, insertText.ToString());
            doc.RequestRedraw();
        }

        public string GetParsedSnipped(string data,Encoding enc,int tabNum)
        {
            //インテンドの数を計算する
            StringBuilder tabs = new StringBuilder();
            for (int i = 0; i < tabNum; i++)
                tabs.Append("\t");

            //トークンを解析する
            StringBuilder insertText = new StringBuilder();
            string[] snipetLines = this.Data.Split(new string[] { "\\n" }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < snipetLines.Length; i++)
            {
                string[] oldValues = new string[] { "${encode}", "\\t", "\\i" };
                string[] newValues = new string[] { enc.WebName, "\t", i == 0 ? "" : tabs.ToString() };  //最初の行はインデンド済みなので空文字を返す
                insertText.Append(Util.Replace(snipetLines[i], oldValues, newValues));
                insertText.Append(Document.NewLine);
            }

            return insertText.ToString();
        }
    }

    sealed class Util
    {
        public static string Replace(string s, string[] oldValues, string[] newValues)
        {
            if (oldValues.Length != newValues.Length)
                throw new ArgumentException("oldValuesとnewValuesの数が一致しません");

            StringBuilder str = new StringBuilder(s);
            for (int i = 0; i < oldValues.Length; i++)
                str = str.Replace(oldValues[i], newValues[i]);
            return str.ToString();
        }
    }
}
