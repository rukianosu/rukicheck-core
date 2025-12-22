using System.Windows;
using RukiCheck.ViewModels;

namespace RukiCheck.Views;

public partial class CameraTestWindow : Window
{
    private readonly CameraTestViewModel _viewModel;

    public CameraTestWindow(CameraTestViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = _viewModel;

        // 完了時にウィンドウを閉じる
        _viewModel.OnCompleted = () => DialogResult = true;
    }
}
