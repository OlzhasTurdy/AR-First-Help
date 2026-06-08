using System.Collections;
using UnityEngine;
using UnityEngine.Video;

/// <summary>
/// Добавьте этот компонент на любой GameObject (например, ScenarioController).
/// Привяжите панель с Video Player в Inspector.
/// Вызывайте из ScenarioController.ShowStep() метод ShowVideoPanel() / HideVideoPanel().
/// </summary>
public class VideoPanelAnimator : MonoBehaviour
{
    [Header("Video Panel")]
    public RectTransform videoPanel;        // Сам белый Panel с Video Player
    public VideoPlayer videoPlayer;         // Video Player компонент внутри панели

    [Header("Positions (Pos Y)")]
    public float shownPosY = -331f;        // Позиция Y когда панель ВИДНА
    public float hiddenPosY = 400f;        // Позиция Y когда панель СКРЫТА

    [Header("Animation")]
    public float slideDuration = 0.35f;     // Секунды анимации
    public AnimationCurve slideCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    private Coroutine _slideCoroutine;
    private bool _isPanelVisible = false;

    void Awake()
    {
        // Начальное состояние — скрыта
        SetPanelY(hiddenPosY);
    }

    // ─── Публичные методы — вызывайте из ScenarioController ─────────

    /// <summary>Показать панель с видео и запустить клип.</summary>
    public void ShowVideoPanel(VideoClip clip)
    {
        if (videoPlayer != null && clip != null)
        {
            videoPlayer.source = VideoSource.VideoClip;
            videoPlayer.clip = clip;
            videoPlayer.audioOutputMode = VideoAudioOutputMode.None;
            videoPlayer.Play();
        }

        SlidePanel(shownPosY);
        _isPanelVisible = true;
    }

    /// <summary>Показать панель и воспроизвести видео по URL (для кастомных сценариев).</summary>
    public void ShowVideoPanelFromUrl(string url)
    {
        if (videoPlayer != null && !string.IsNullOrEmpty(url))
        {
            videoPlayer.source = VideoSource.Url;
            videoPlayer.url = url;
            videoPlayer.audioOutputMode = VideoAudioOutputMode.None;
            videoPlayer.Play();
        }

        SlidePanel(shownPosY);
        _isPanelVisible = true;
    }

    /// <summary>Скрыть панель и остановить воспроизведение.</summary>
    public void HideVideoPanel()
    {
        if (videoPlayer != null)
            videoPlayer.Stop();

        SlidePanel(hiddenPosY);
        _isPanelVisible = false;
    }

    // ─── Внутренняя логика ───────────────────────────────────────────

    void SlidePanel(float targetY)
    {
        if (_slideCoroutine != null)
            StopCoroutine(_slideCoroutine);

        _slideCoroutine = StartCoroutine(SlideTo(targetY));
    }

    IEnumerator SlideTo(float targetY)
    {
        float startY = videoPanel.anchoredPosition.y;
        float elapsed = 0f;

        while (elapsed < slideDuration)
        {
            elapsed += Time.unscaledDeltaTime;   // unscaled — работает даже при паузе
            float t = slideCurve.Evaluate(elapsed / slideDuration);
            SetPanelY(Mathf.Lerp(startY, targetY, t));
            yield return null;
        }

        SetPanelY(targetY);
    }

    void SetPanelY(float y)
    {
        if (videoPanel == null) return;
        var pos = videoPanel.anchoredPosition;
        pos.y = y;
        videoPanel.anchoredPosition = pos;
    }
}