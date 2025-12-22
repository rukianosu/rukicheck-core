using System.Windows;
using RukiCheck.ViewModels;

namespace RukiCheck.Views;

public partial class MicrophoneTestWindow : Window
{
    private readonly MicrophoneTestViewModel _viewModel;

    public MicrophoneTestWindow(MicrophoneTestViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = _viewModel;

        // 完了時にウィンドウを閉じる
        _viewModel.OnCompleted = () => DialogResult = true;
    }
}
