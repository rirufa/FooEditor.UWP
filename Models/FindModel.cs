using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using FooEditEngine;

namespace FooEditor.UWP.Models
{
    /// <summary>
    /// 検索しても何も見つからなかったことを表します
    /// </summary>
    class NotFoundExepction : Exception
    {

    }

    class FindModel
    {
        static Color foundMarkerColor = new Color(64, 128, 128, 128);
        bool canReplaceNext = false;
        IEnumerator<Tuple<Document, SearchResult>> iterator;

        public DocumentCollection DocumentCollection
        {
            get;
            set;
        }

        public bool AllDocuments
        {
            get;
            set;
        }

        public void FindNext(string pattern, bool useregex, RegexOptions opt)
        {
            if (this.iterator == null)
            {
                this.iterator = this.GetSearchResult((document) =>
                {
                    if (document.Length == 0)
                        throw new NotFoundExepction();
                    document.SetFindParam(pattern, useregex, opt);
                    if (AppSettings.Current.ShowFoundPattern)
                    {
                        document.MarkerPatternSet.Remove(DocumentModel.FoundMarkerID);
                        document.MarkerPatternSet.Add(DocumentModel.FoundMarkerID, document.CreateWatchDogByFindParam(HilightType.Select, foundMarkerColor));
                    }
                    return document.Find();
                });
            }
            if (!this.iterator.MoveNext())
            {
                this.canReplaceNext = false;
                this.iterator = null;
                foreach (Document textBox in this.GetTextBoxs())
                    textBox.MarkerPatternSet.Remove(DocumentModel.FoundMarkerID);
                throw new NotFoundExepction();
            }
            else
            {
                SearchResult sr = this.iterator.Current.Item2;
                Document textBox = this.iterator.Current.Item1;
                textBox.CaretPostion = textBox.LayoutLines.GetTextPointFromIndex(sr.End + 1);
                textBox.Select(sr.Start, sr.End - sr.Start + 1);
                textBox.RequestRedraw();
                this.canReplaceNext = true;
            }
        }

        public void Replace(string newpattern, bool usegroup)
        {
            if (!this.canReplaceNext)
                return;
            if (newpattern == null)
                newpattern = string.Empty;
            SearchResult sr = this.iterator.Current.Item2;
            Document textBox = this.iterator.Current.Item1;
            var newText = usegroup ? sr.Result(newpattern) : newpattern;
            textBox.Replace(sr.Start, sr.End - sr.Start + 1, newText);
            textBox.RequestRedraw();
        }

        public void ReplaceAll(string pattern, string newpattern, bool usegroup, bool useregex, RegexOptions opt)
        {
            if (newpattern == null)
                newpattern = string.Empty;
            foreach (Document Document in this.GetTextBoxs())
            {
                //キャレット位置をドキュメントに先頭に移動させないと落ちる
                Document.CaretPostion = new TextPoint(0, 0);
                Document.FireUpdateEvent = false;
                try
                {
                    if (useregex)
                    {
                        Document.SetFindParam(pattern, useregex, opt);
                        Document.ReplaceAll(newpattern, usegroup);
                    }
                    else
                    {
                        Document.ReplaceAll2(pattern, newpattern, (opt & RegexOptions.IgnoreCase) == RegexOptions.IgnoreCase);
                    }
                }
                finally
                {
                    Document.FireUpdateEvent = true;
                }
                Document.RequestRedraw();
            }
        }

        public void Reset()
        {
            this.iterator = null;
        }

        /// <summary>
        /// イテレーターを生成する
        /// </summary>
        /// <returns>イテレーター</returns>
        protected virtual IEnumerable<Document> GetTextBoxs()
        {
            foreach (var docvm in this.DocumentCollection)
                yield return docvm.DocumentModel.Document;
        }

        /// <summary>
        /// イテレーターを生成する
        /// </summary>
        /// <param name="FindStartFunc">検索開始時に実行される関数</param>
        /// <returns>Tuple<FooTextBox, SearchResult>イテレーター</returns>
        /// <remarks>必ずオーバーライトする必要があります。オーバーライトする際はFindStartFuncを呼び出してください</remarks>
        protected virtual IEnumerator<Tuple<Document, SearchResult>> GetSearchResult(Func<Document, IEnumerator<SearchResult>> FindStartFunc)
        {
            if (this.AllDocuments)
            {
                foreach (var docinfo in this.DocumentCollection)
                {
                    this.DocumentCollection.ActiveDocument(docinfo);

                    IEnumerator<SearchResult> it = FindStartFunc(docinfo.DocumentModel.Document);
                    while (it.MoveNext())
                    {
                        yield return new Tuple<Document, SearchResult>(docinfo.DocumentModel.Document, it.Current);
                    }
                }
            }
            else
            {
                var activeDocument = this.DocumentCollection.Current.DocumentModel.Document;
                IEnumerator<SearchResult> it = FindStartFunc(activeDocument);
                while (it.MoveNext())
                {
                    yield return new Tuple<Document, SearchResult>(activeDocument, it.Current);
                }
            }
        }
    }
}
