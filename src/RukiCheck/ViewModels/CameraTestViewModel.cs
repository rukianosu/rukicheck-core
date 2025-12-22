using System.IO;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using RukiCheck.Services;

namespace RukiCheck.ViewModels;

/// <summary>
/// カメラ検査ウィンドウのViewModel
/// </summary>
public class CameraTestViewModel : ViewModelBase
{
    private readonly CameraInspectionService _service;
    private readonly DispatcherTimer _previewTimer;

    private BitmapImage? _previewImage;
    private bool _isCameraOpen = false;
    private bool _isCaptured = false;
    private string _statusMessage = "カメラを起動しています...";

    public CameraTestViewModel(CameraInspectionService service)
    {
        _service = service;

        // コマンド
        CaptureCommand = new RelayCommand(_ => OnCapture(), _ => IsCameraOpen && !IsCaptured);
        RetakeCommand = new RelayCommand(_ => OnRetake(), _ => IsCaptured);
        CompleteCommand = new RelayCommand(_ => OnComplete(), _ => IsCaptured);

        // プレビュー用タイマー（30fps）
        _previewTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(33)
        };
        _previewTimer.Tick += PreviewTimer_Tick;

        // カメラを開く
        StartCamera();
    }

    #region Properties

    public BitmapImage? PreviewImage
    {
        get => _previewImage;
        set => SetProperty(ref _previewImage, value);
    }

    public bool IsCameraOpen
    {
        get => _isCameraOpen;
        set
        {
            if (SetProperty(ref _isCameraOpen, value))
            {
                CommandManager.InvalidateRequerySuggested();
            }
        }
    }

    public bool IsCaptured
    {
        get => _isCaptured;
        set
        {
            if (SetProperty(ref _isCaptured, value))
            {
                CommandManager.InvalidateRequerySuggested();
            }
        }
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    #endregion

    #region Commands

    public RelayCommand CaptureCommand { get; }
    public RelayCommand RetakeCommand { get; }
    public RelayCommand CompleteCommand { get; }

    public Action? OnCompleted { get; set; }

    #endregion

    #region Methods

    private void StartCamera()
    {
        if (_service.OpenCamera())
        {
            IsCameraOpen = true;
            StatusMessage = "カメラが正常に起動しました。「キャプチャ」ボタンを押してください";
            _previewTimer.Start();
        }
        else
        {
            IsCameraOpen = false;
            StatusMessage = "❌ カメラを開けませんでした。カメラが接続されているか確認してください";
        }
    }

    private void PreviewTimer_Tick(object? sender, EventArgs e)
    {
        if (!IsCameraOpen || IsCaptured)
            return;

        var frameBytes = _service.GrabFrame();
        if (frameBytes != null)
        {
            PreviewImage = BytesToBitmapImage(frameBytes);
        }
    }

    private void OnCapture()
    {
        // プレビューを停止
        _previewTimer.Stop();

        // 静止画をキャプチャ
        var imagePath = _service.CaptureImage();
        if (imagePath != null && File.Exists(imagePath))
        {
            // キャプチャした画像を表示
            PreviewImage = new BitmapImage(new Uri(imagePath));
            IsCaptured = true;
            StatusMessage = "✅ キャプチャが完了しました！「完了」ボタンを押してください";
        }
        else
        {
            StatusMessage = "❌ キャプチャに失敗しました";
            _previewTimer.Start();
        }
    }

    private void OnRetake()
    {
        IsCaptured = false;
        StatusMessage = "プレビューを再開しました";
        _previewTimer.Start();
    }

    private void OnComplete()
    {
        _previewTimer.Stop();
        _service.CloseCamera();
        OnCompleted?.Invoke();
    }

    private BitmapImage BytesToBitmapImage(byte[] bytes)
    {
        using var ms = new MemoryStream(bytes);
        var bitmap = new BitmapImage();
        bitmap.BeginInit();
        bitmap.CacheOption = BitmapCacheOption.OnLoad;
        bitmap.StreamSource = ms;
        bitmap.EndInit();
        bitmap.Freeze();
        return bitmap;
    }

    #endregion
}
