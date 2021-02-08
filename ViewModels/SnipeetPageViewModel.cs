using System.Collections.ObjectModel;
using System.Windows.Input;
using Prism.Windows.Mvvm;
using Prism.Commands;
using FooEditEngine;
using FooEditor.UWP.Models;
using System.Threading.Tasks;
using Prism.Windows.Navigation;
using System.Collections.Generic;
using System.Linq;

namespace FooEditor.UWP.ViewModels
{
    class SnipeetPageViewModel : ViewModelBase,INavigationAware
    {
        public override async void OnNavigatedTo(NavigatedToEventArgs e, Dictionary<string, object> viewModelState)
        {
            await this.OnLoadCategories();
        }

        ObservableCollection<SnippetCategoryViewModel> _CategoryList;
        public ObservableCollection<SnippetCategoryViewModel> CategoryList
        {
            get
            {
                return this._CategoryList;
            }
            set
            {
                SetProperty(ref _CategoryList, value);
            }
        }

        SnippetCategoryViewModel _CurrentCategory;
        public SnippetCategoryViewModel CurrentCategory
        {
            get
            {
                return this._CurrentCategory;
            }
            set
            {
                SetProperty(ref _CurrentCategory, value);
                this.OnChangeCategory();
            }
        }

        ObservableCollection<SnippetViewModel> _Snippets;
        public ObservableCollection<SnippetViewModel> Snippets
        {
            get
            {
                return this._Snippets;
            }
            set
            {
                SetProperty(ref this._Snippets, value);
            }
        }

        SnippetViewModel _SelectSnippet;
        public SnippetViewModel SelectSnippet
        {
            get
            {
                return this._SelectSnippet;
            }
            set
            {
                SetProperty(ref _SelectSnippet, value);
            }
        }

        private async Task OnLoadCategories()
        {
            this.CategoryList = await SnipeetLoader.LoadCategory();
        }

        private async void OnChangeCategory()
        {
            this.Snippets = await SnipeetLoader.LoadSnippets(this.CurrentCategory.FilePath);
        }

        public DelegateCommand<object> InsertSnippetCommand
        {
            get
            {
                return new DelegateCommand<object>((s) => {
                    this.SelectSnippet.InsetToDocument(DocumentCollection.Instance.Current.DocumentModel);
                });
            }
        }
    }
}
