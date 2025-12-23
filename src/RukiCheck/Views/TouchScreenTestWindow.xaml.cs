using System.Windows;
using System.Windows.Input;
using RukiCheck.ViewModels;

namespace RukiCheck.Views;

public partial class TouchScreenTestWindow : Window
{
    private readonly TouchScreenTestViewModel? _viewModel;

    public TouchScreenTestWindow(TouchScreenTestViewModel? viewModel = null)
    {
        InitializeComponent();

        _viewModel = viewModel;
        if (_viewModel != null)
        {
            DataContext = _viewModel;
            _viewModel.OnCompleted = () =>
            {
                DialogResult = true;
                Close();
            };
        }
    }

    private void TouchTestCanvas_TouchDown(object sender, TouchEventArgs e)
    {
        if (_viewModel == null) return;

        var position = e.GetTouchPoint(TouchTestCanvas).Position;
        _viewModel.OnTouchDown(position);

        e.Handled = true;
    }

    private void TouchTestCanvas_TouchUp(object sender, TouchEventArgs e)
    {
        if (_viewModel == null) return;

        var position = e.GetTouchPoint(TouchTestCanvas).Position;
        _viewModel.OnTouchUp(position);

        e.Handled = true;
    }

    private void TouchTestCanvas_TouchMove(object sender, TouchEventArgs e)
    {
        // タッチムーブは現在未使用（将来的にスワイプ検出の精度向上に使用可能）
        e.Handled = true;
    }
}
