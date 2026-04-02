using UnityEngine;
using UnityEngine.UI;
using Mediapipe.Unity.Sample.PoseLandmarkDetection;
using Mediapipe.Tasks.Vision.PoseLandmarker;

public class CPRHighlighter : MonoBehaviour
{
    [Header("Основные ссылки")]
    public PoseLandmarkerRunner runner;
    public RectTransform uiRedZone; // Ваш Image 'ChestIndicator' в Canvas

    [Header("Настройка позиции")]
    [Range(0, 1)] public float chestDownOffset = 0.32f; // Смещение от плеч к бедрам
    public float uiScaleMultiplier = 1.0f;
    public float distanceFromCamera = 2.0f; // Дистанция "виртуальной" точки от линзы

    private Canvas parentCanvas;
    private Camera mainCam;

    void Start()
    {
        mainCam = Camera.main;
        if (uiRedZone != null)
        {
            parentCanvas = uiRedZone.GetComponentInParent<Canvas>();
            uiRedZone.pivot = new Vector2(0.5f, 0.5f);
        }
    }

    void Update()
    {
        // 1. Проверка данных
        if (runner == null || runner.LatestResult.poseLandmarks == null ||
            runner.LatestResult.poseLandmarks.Count == 0)
        {
            if (uiRedZone.gameObject.activeSelf) uiRedZone.gameObject.SetActive(false);
            return;
        }

        var landmarks = runner.LatestResult.poseLandmarks[0].landmarks;
        if (landmarks.Count < 33) return;

        if (!uiRedZone.gameObject.activeSelf) uiRedZone.gameObject.SetActive(true);

        // 2. Получаем мировые позиции ключевых точек
        // Мы используем ViewportToWorldPoint, чтобы учесть перспективу камеры
        Vector3 worldLShoulder = GetWorldPos(landmarks[11]);
        Vector3 worldRShoulder = GetWorldPos(landmarks[12]);
        Vector3 worldLHip = GetWorldPos(landmarks[23]);
        Vector3 worldRHip = GetWorldPos(landmarks[24]);

        // 3. Считаем центр груди в 3D пространстве
        Vector3 worldShoulderMid = (worldLShoulder + worldRShoulder) / 2f;
        Vector3 worldHipMid = (worldLHip + worldRHip) / 2f;

        // Точка на груди (интерполяция в мировых координатах)
        Vector3 worldChestPoint = Vector3.Lerp(worldShoulderMid, worldHipMid, chestDownOffset);

        // 4. Проецируем 3D точку обратно на экран (UI)
        Vector2 screenPoint = mainCam.WorldToScreenPoint(worldChestPoint);

        // Учитываем масштаб Canvas, если он в режиме Scale With Screen Size
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parentCanvas.transform as RectTransform,
            screenPoint,
            parentCanvas.worldCamera,
            out Vector2 localPoint);

        uiRedZone.anchoredPosition = localPoint;

        // 5. Динамический размер (зависит от расстояния между плечами в 3D)
        float shoulderDist = Vector3.Distance(worldLShoulder, worldRShoulder);
        // Масштабируем UI в зависимости от того, насколько "широкие" плечи видит камера
        float finalSize = (shoulderDist * 100f) * uiScaleMultiplier;
        uiRedZone.sizeDelta = new Vector2(finalSize, finalSize);
    }

    // Вспомогательная функция для перевода координат MediaPipe в Мир Unity
    Vector3 GetWorldPos(Mediapipe.Tasks.Components.Containers.NormalizedLandmark mark)
    {
        // Поскольку видео с телефона уже отзеркалено, берем X как есть (mark.x)
        // Если квадрат всё равно "инвертирован", замените на (1f - mark.x)
        float x = mark.x;
        float y = 1f - mark.y; // MediaPipe Y инвертирован относительно Unity

        // Создаем точку во Viewport (0..1) и переводим в World
        return mainCam.ViewportToWorldPoint(new Vector3(x, y, distanceFromCamera));
    }
}