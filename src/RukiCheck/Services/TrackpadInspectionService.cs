using RukiCheck.Models;

namespace RukiCheck.Services;

/// <summary>
/// トラックパッド検査サービス
/// </summary>
public class TrackpadInspectionService : IInspectionService<TrackpadResult>
{
    private bool _cursorMoved = false;
    private bool _leftClicked = false;
    private bool _rightClicked = false;
    private bool _scrollDetected = false;

    /// <summary>
    /// カーソル移動を記録
    /// </summary>
    public void RecordCursorMove()
    {
        _cursorMoved = true;
    }

    /// <summary>
    /// 左クリックを記録
    /// </summary>
    public void RecordLeftClick()
    {
        _leftClicked = true;
    }

    /// <summary>
    /// 右クリックを記録
    /// </summary>
    public void RecordRightClick()
    {
        _rightClicked = true;
    }

    /// <summary>
    /// スクロールを記録
    /// </summary>
    public void RecordScroll()
    {
        _scrollDetected = true;
    }

    /// <summary>
    /// 検査結果を生成
    /// </summary>
    public Task<TrackpadResult> ExecuteAsync(string attachmentPath)
    {
        var result = new TrackpadResult
        {
            CursorMoved = _cursorMoved,
            LeftClick = _leftClicked,
            RightClick = _rightClicked,
            ScrollDetected = _scrollDetected
        };

        try
        {
            // 判定ロジック
            int passedTests = 0;
            if (_cursorMoved) passedTests++;
            if (_leftClicked) passedTests++;
            if (_rightClicked) passedTests++;
            if (_scrollDetected) passedTests++;

            if (passedTests == 4)
            {
                result.Result = "pass";
                result.Note = "すべてのテストに合格しました";
            }
            else if (passedTests >= 2)
            {
                result.Result = "warn";
                result.Note = $"{passedTests}/4 項目が動作しました";
            }
            else
            {
                result.Result = "fail";
                result.Note = $"複数の機能が動作していません（{passedTests}/4）";
            }
        }
        catch (Exception ex)
        {
            result.Error = $"検査失敗: {ex.Message}";
            result.Result = "error";
        }

        return Task.FromResult(result);
    }

    /// <summary>
    /// 検査状態をリセット
    /// </summary>
    public void Reset()
    {
        _cursorMoved = false;
        _leftClicked = false;
        _rightClicked = false;
        _scrollDetected = false;
    }
}
