using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Practices.Unity;
using Prism.Commands;
using Prism.Windows.Mvvm;
using FooEditEngine;
using FooEditor.UWP.Models;
using Prism.Windows.Navigation;
using Windows.ApplicationModel.DataTransfer;

namespace FooEditor.UWP.ViewModels
{
    class OutlinePageViewModel : ViewModelBase
    {
        public OutlinePageViewModel()
        {
        }

        [InjectionConstructor]
        public OutlinePageViewModel(INavigationService nav)
        {
            this.DocumentList = DocumentCollection.Instance;
            this.DocumentList.ActiveDocumentChanged += DocumentList_ActiveDocumentChanged;
        }

        private DocumentCollection DocumentList;
        private Document Document;
        private IFoldingStrategy FoldingStrategy;

        ObservableCollection<OutlineTreeItem> _Items;
        public ObservableCollection<OutlineTreeItem> Items
        {
            get
            {
                return _Items;
            }
            set
            {
                SetProperty(ref _Items, value);
            }
        }

        private void DocumentList_ActiveDocumentChanged(object sender, DocumentCollectionEventArgs e)
        {
            var doclist = (DocumentCollection)sender;
            this.Document = doclist.Current.DocumentModel.Document;
            this.FoldingStrategy = doclist.Current.DocumentModel.FoldingStrategy;
            if (this.Items != null)
                this.Items.Clear();
        }

        public DelegateCommand<object> AnalyzeCommand
        {
            get
            {
                return new DelegateCommand<object>((s) => {
                    this.Document = this.DocumentList.Current.DocumentModel.Document;
                    this.FoldingStrategy = this.DocumentList.Current.DocumentModel.FoldingStrategy;
                    this.Items = OutlineAnalyzer.Analyze(this.FoldingStrategy, this.Document.LayoutLines, this.Document);
                });
            }
        }

        public DelegateCommand<OutlineTreeItem> JumpCommand
        {
            get
            {
                return new DelegateCommand<OutlineTreeItem>((s) =>
                {
                    OutlineTreeItem item = s;
                    OutlineActions.JumpNode(item, this.Document);
                });
            }
        }

        public DelegateCommand<OutlineTreeItem> CutCommand
        {
            get
            {
                return new DelegateCommand<OutlineTreeItem>((s) =>
                {
                    OutlineTreeItem treeitem = s;
                    this.SetToClipboard(treeitem);
                    this.Document.Remove(treeitem.Start, treeitem.End - treeitem.Start + 1);
                    this.Document.RequestRedraw();

                    this.Items = OutlineAnalyzer.Analyze(this.FoldingStrategy, this.Document.LayoutLines, this.Document);
                });
            }
        }

        public DelegateCommand<OutlineTreeItem> CopyCommand
        {
            get
            {
                return new DelegateCommand<OutlineTreeItem>((s) =>
                {
                    OutlineTreeItem treeitem = s;
                    this.SetToClipboard(treeitem);
                });
            }
        }

        public DelegateCommand<OutlineTreeItem> PasteAsChildCommand
        {
            get
            {
                return new DelegateCommand<OutlineTreeItem>(async (s) =>
                {
                    OutlineTreeItem treeitem = s;
                    var view = Clipboard.GetContent();
                    string text = OutlineActions.FitOutlineLevel(await view.GetTextAsync(), treeitem, treeitem.Level + 1);
                    this.Document.Replace(treeitem.End + 1, 0, text);
                    this.Document.RequestRedraw();

                    this.Items = OutlineAnalyzer.Analyze(this.FoldingStrategy, this.Document.LayoutLines, this.Document);
                });
            }
        }

        public DelegateCommand<OutlineTreeItem> UpLevelCommand
        {
            get
            {
                return new DelegateCommand<OutlineTreeItem>((s) =>
                {
                    if (this.FoldingStrategy is WZTextFoldingGenerator)
                    {
                        OutlineTreeItem item = s;
                        string text = this.Document.ToString(item.Start, item.End - item.Start + 1);
                        text = OutlineActions.FitOutlineLevel(text, item, item.Level + 1);

                        Document.Replace(item.Start, item.End - item.Start + 1, text);
                        Document.RequestRedraw();

                        this.Items = OutlineAnalyzer.Analyze(this.FoldingStrategy, this.Document.LayoutLines, this.Document);
                    }
                },
                (s) => {
                    return this.FoldingStrategy is WZTextFoldingGenerator;
                });
            }
        }

        public DelegateCommand<OutlineTreeItem> DownLevelCommand
        {
            get
            {
                return new DelegateCommand<OutlineTreeItem>((s) =>
                {
                    if(this.FoldingStrategy is WZTextFoldingGenerator)
                    {
                        OutlineTreeItem item = s;
                        string text = this.Document.ToString(item.Start, item.End - item.Start + 1);
                        text = OutlineActions.FitOutlineLevel(text, item, item.Level - 1);

                        Document.Replace(item.Start, item.End - item.Start + 1, text);
                        Document.RequestRedraw();

                        this.Items = OutlineAnalyzer.Analyze(this.FoldingStrategy, this.Document.LayoutLines, this.Document);
                    }
                },
                (s)=> {
                    return this.FoldingStrategy is WZTextFoldingGenerator;
                });
            }
        }

        void SetToClipboard(OutlineTreeItem treeitem)
        {
            string text = this.Document.ToString(treeitem.Start, treeitem.End - treeitem.Start + 1);
            var dataPackage = new DataPackage();
            dataPackage.RequestedOperation = DataPackageOperation.Copy;
            dataPackage.SetText(text);
            Clipboard.SetContent(dataPackage);
        }
    }
}
