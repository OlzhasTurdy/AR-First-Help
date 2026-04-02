using UnityEngine;
using Mediapipe.Tasks.Vision.PoseLandmarker;
// Если эта строка выдает ошибку, убедись, что скрипт лежит в той же папке, 
// что и PoseLandmarkerRunner.cs, либо удали её и используй автозаполнение
using Mediapipe.Unity.Sample.PoseLandmarkDetection;

public class BodyTrackingBridge : MonoBehaviour
{
    [Header("Ссылки")]
    [Tooltip("Перетащи сюда объект Solution из сцены")]
    public PoseLandmarkerRunner runner;

    [Tooltip("Твой красный объект (сфера/квадрат), который будет на груди")]
    public GameObject chestOverlay;

    [Header("Настройки позиции")]
    [Tooltip("Расстояние от камеры до красной точки (в метрах)")]
    public float distanceFromCamera = 1.5f;

    [Range(0, 0.5f)]
    [Tooltip("На сколько опустить точку ниже линии плеч (0.15 - 0.2 оптимально)")]
    public float chestOffsetDown = 0.18f;

    [Header("Настройки визуала")]
    [Tooltip("Скорость следования точки за человеком (чем выше, тем резче)")]
    public float smoothSpeed = 12f;

    private Camera arCamera;
    private Vector3 targetPosition;

    void Awake()
    {
        // Находим AR камеру. В AR Foundation она обычно имеет тег MainCamera
        arCamera = Camera.main;
    }

    void Update()
    {
        // 1. Проверяем, запущен ли MediaPipe и есть ли данные
        if (runner == null ||
            runner.LatestResult.poseLandmarks == null ||
            runner.LatestResult.poseLandmarks.Count == 0)
        {
            // Если человека не видно — скрываем красную зону
            if (chestOverlay != null && chestOverlay.activeSelf)
                chestOverlay.SetActive(false);
            return;
        }

        // 2. Берем первый найденный скелет (индекс 0)
        var pose = runner.LatestResult.poseLandmarks[0];

        // 3. Получаем плечи (11 - левое, 12 - правое)
        // В этой версии плагина используем .landmarks (с маленькой буквы)
        var leftShoulder = pose.landmarks[11];
        var rightShoulder = pose.landmarks[12];

        // 4. Считаем центр груди (среднее арифметическое между плечами)
        float centerX = (leftShoulder.x + rightShoulder.x) / 2f;
        float centerY = (leftShoulder.y + rightShoulder.y) / 2f;

        // 5. Конвертируем MediaPipe (0,0 сверху-слева) в Unity Viewport (0,0 снизу-слева)
        // Инвертируем Y: 1.0 - centerY
        Vector3 viewportPos = new Vector3(centerX, 1f - centerY, distanceFromCamera);

        // Опускаем точку чуть ниже плеч, чтобы она была на грудине
        viewportPos.y -= chestOffsetDown;

        // 6. Магия AR: переводим 2D координаты экрана в 3D координаты мира
        targetPosition = arCamera.ViewportToWorldPoint(viewportPos);

        // 7. Визуализация
        if (chestOverlay != null)
        {
            if (!chestOverlay.activeSelf) chestOverlay.SetActive(true);

            // Плавно перемещаем объект в целевую точку
            chestOverlay.transform.position = Vector3.Lerp(
                chestOverlay.transform.position,
                targetPosition,
                Time.deltaTime * smoothSpeed
            );

            // Поворачиваем "лицом" к камере
            chestOverlay.transform.LookAt(arCamera.transform);
            chestOverlay.transform.Rotate(0, 180, 0);
        }
    }

    // Метод для управления из ScenarioController
    public void SetOverlayActive(bool active)
    {
        if (chestOverlay != null) chestOverlay.SetActive(active);
        this.enabled = active; // Выключаем сам скрипт, чтобы не тратить ресурсы
    }
}