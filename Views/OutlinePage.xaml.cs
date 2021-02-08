using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Prism.Windows.Mvvm;
using FooEditor.UWP.ViewModels;
using FooEditor.UWP.Models;

namespace FooEditor.UWP.Views
{
    public class AnalyzePattern
    {
        public string Type;
        public string[] Patterns;
        public override string ToString()
        {
            return this.Type;
        }
        public AnalyzePattern(string type, string[] patterns)
        {
            this.Type = type;
            this.Patterns = patterns;
        }
    }
    /// <summary>
    /// OutlineWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class OutlinePage : Page
    {
        public OutlinePage()
        {
            InitializeComponent();
        }

        private void TextBlock_RightTapped(object sender, Windows.UI.Xaml.Input.RightTappedRoutedEventArgs e)
        {
            Flyout.ShowAttachedFlyout(sender as FrameworkElement);
        }

        private void TextBlock_DoubleTapped(object sender, Windows.UI.Xaml.Input.DoubleTappedRoutedEventArgs e)
        {
            OutlinePageViewModel vm = this.DataContext as OutlinePageViewModel;
            vm.JumpCommand.Execute(this.TreeView.SelectedItem as OutlineTreeItem);
        }
    }
}
