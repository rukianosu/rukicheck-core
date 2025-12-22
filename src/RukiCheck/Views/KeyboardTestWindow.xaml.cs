using System.Windows;
using System.Windows.Input;
using RukiCheck.ViewModels;

namespace RukiCheck.Views;

public partial class KeyboardTestWindow : Window
{
    private readonly KeyboardTestViewModel _viewModel;

    public KeyboardTestWindow(KeyboardTestViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = _viewModel;

        // 完了時にウィンドウを閉じる
        _viewModel.OnCompleted = () => DialogResult = true;
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        // キー押下を処理
        _viewModel.HandleKeyPress(e.Key);
    }
}
