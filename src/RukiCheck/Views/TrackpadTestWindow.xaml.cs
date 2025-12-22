using System.Windows;
using System.Windows.Input;
using RukiCheck.ViewModels;

namespace RukiCheck.Views;

public partial class TrackpadTestWindow : Window
{
    private readonly TrackpadTestViewModel _viewModel;

    public TrackpadTestWindow(TrackpadTestViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = _viewModel;

        // 完了時にウィンドウを閉じる
        _viewModel.OnCompleted = () => DialogResult = true;
    }

    private void Window_MouseMove(object sender, MouseEventArgs e)
    {
        _viewModel.HandleMouseMove();
    }

    private void Window_LeftClick(object sender, MouseButtonEventArgs e)
    {
        _viewModel.HandleLeftClick();
    }

    private void Window_RightClick(object sender, MouseButtonEventArgs e)
    {
        _viewModel.HandleRightClick();
        e.Handled = true; // コンテキストメニューを表示しない
    }

    private void Window_Scroll(object sender, MouseWheelEventArgs e)
    {
        _viewModel.HandleScroll();
    }
}
