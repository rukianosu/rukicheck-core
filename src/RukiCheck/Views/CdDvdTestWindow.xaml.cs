using System.Windows;
using RukiCheck.ViewModels;

namespace RukiCheck.Views;

public partial class CdDvdTestWindow : Window
{
    private readonly CdDvdTestViewModel _viewModel;

    public CdDvdTestWindow(CdDvdTestViewModel viewModel)
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

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
