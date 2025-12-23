using System.Windows;
using RukiCheck.ViewModels;

namespace RukiCheck.Views;

public partial class MemoryTestWindow : Window
{
    private readonly MemoryTestViewModel _viewModel;

    public MemoryTestWindow(MemoryTestViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = _viewModel;

        // 完了時にウィンドウを閉じる
        _viewModel.OnCompleted = () => DialogResult = true;
    }

    private void CompleteButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }
}
