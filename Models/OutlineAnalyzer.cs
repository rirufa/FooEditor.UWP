using System.Collections.ObjectModel;
using FooEditEngine;

namespace FooEditor.UWP.Models
{
    public sealed class OutlineTreeItem
    {
        /// <summary>
        /// コンストラクター
        /// </summary>
        public OutlineTreeItem(int start, int end, int level, string header)
        {
            this.Start = start;
            this.End = end;
            this.Level = level;
            this.Header = header;
            this.Items = new ObservableCollection<OutlineTreeItem>();
        }
        /// <summary>
        /// 開始インデックス
        /// </summary>
        public int Start
        {
            get;
            private set;
        }
        /// <summary>
        /// 終了インデックス
        /// </summary>
        public int End
        {
            get;
            private set;
        }
        /// <summary>
        /// アウトラインレベル
        /// </summary>
        public int Level
        {
            get;
            private set;
        }
        /// <summary>
        /// 表題
        /// </summary>
        public string Header
        {
            get;
            private set;
        }
        /// <summary>
        /// サブノード
        /// </summary>
        public ObservableCollection<OutlineTreeItem> Items
        {
            get;
            private set;
        }
    }
    class OutlineAnalyzer
    {
        /// <summary>
        /// アウトラインツリーを生成し、TreeViewに追加する
        /// </summary>
        public static ObservableCollection<OutlineTreeItem> Analyze(IFoldingStrategy foldingMethod,LineToIndexTable layoutlineCollection,Document doc)
        {
            if (foldingMethod == null)
                return null;
            
            int level = -1;
            ObservableCollection<OutlineTreeItem>[] levels = new ObservableCollection<OutlineTreeItem>[6];
            for (int i = 0; i < levels.Length; i++)
                levels[i] = new ObservableCollection<OutlineTreeItem>();

            foreach (OutlineItem item in foldingMethod.AnalyzeDocument(doc, 0, doc.Length - 1))
            {
                int row = layoutlineCollection.GetLineNumberFromIndex(item.Start);
                string header = layoutlineCollection[row].Trim(new char[] { ' ', '\t', '.', Document.NewLine });
                OutlineTreeItem newItem = new OutlineTreeItem(item.Start,item.End,item.Level,header);
                if (level == -1 || level < item.Level)
                {
                    level = item.Level;
                }
                else if (level > item.Level)
                {
                    level = item.Level;
                    foreach (OutlineTreeItem childItem in levels[level + 1])
                        newItem.Items.Add(childItem);
                    levels[level + 1] = new ObservableCollection<OutlineTreeItem>();
                }
                levels[item.Level].Add(newItem);
            }
            
            ObservableCollection<OutlineTreeItem> root = new ObservableCollection<OutlineTreeItem>();
            foreach (OutlineTreeItem childItem in levels[0])
                root.Add(childItem);

            return root;
        }

        /// <summary>
        /// WZText形式ののアウトラインレベルを取得する
        /// </summary>
        public static int GetWZTextLevel(string str)
        {
            int level = -1;
            for (int i = 0; i < str.Length; i++)
            {
                if (str[i] == '.')
                    level++;
                else
                    break;
            }
            return level;
        }

    }
}
