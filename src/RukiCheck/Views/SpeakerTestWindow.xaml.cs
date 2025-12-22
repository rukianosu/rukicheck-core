using System.Windows;
using RukiCheck.ViewModels;

namespace RukiCheck.Views;

public partial class SpeakerTestWindow : Window
{
    private readonly SpeakerTestViewModel _viewModel;

    public SpeakerTestWindow(SpeakerTestViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = _viewModel;

        // 完了時にウィンドウを閉じる
        _viewModel.OnCompleted = () => DialogResult = true;
    }
}
