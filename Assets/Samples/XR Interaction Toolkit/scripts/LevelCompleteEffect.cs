using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI-эффект блёсток/конфетти при прохождении уровня.
/// Создаёт частицы-звёздочки, которые летят с боков экрана.
/// 
/// Использование:
///   1. Добавьте этот скрипт на Canvas (или пустой GameObject внутри Canvas).
///   2. Перетащите его в поле levelCompleteEffect в ScenarioController.
///   3. Вызовите Play() когда уровень пройден.
/// </summary>
public class LevelCompleteEffect : MonoBehaviour
{
    [Header("Настройки частиц")]
    [Tooltip("Сколько частиц создать на каждой стороне")]
    public int particlesPerSide = 25;
    
    [Tooltip("Общая длительность эффекта в секундах")]
    public float duration = 3f;
    
    [Tooltip("Скорость полёта частиц")]
    public float speed = 800f;
    
    [Tooltip("Размер частиц (мин/макс)")]
    public float minSize = 15f;
    public float maxSize = 40f;

    [Header("Цвета блёсток")]
    public Color[] sparkleColors = new Color[]
    {
        new Color(1f, 0.84f, 0f, 1f),      // Золотой
        new Color(1f, 1f, 1f, 1f),          // Белый
        new Color(0.4f, 0.85f, 0.95f, 1f),  // Голубой
        new Color(1f, 0.5f, 0.8f, 1f),      // Розовый
        new Color(0.6f, 1f, 0.6f, 1f),      // Зелёный
        new Color(1f, 0.65f, 0.2f, 1f),     // Оранжевый
        new Color(0.7f, 0.5f, 1f, 1f),      // Фиолетовый
    };

    // Внутренние переменные
    private Canvas parentCanvas;
    private RectTransform canvasRect;

    void Awake()
    {
        // Ищем Canvas в родителях
        parentCanvas = GetComponentInParent<Canvas>();
        if (parentCanvas != null)
            canvasRect = parentCanvas.GetComponent<RectTransform>();
    }

    /// <summary>
    /// Запустить эффект блёсток с боков экрана.
    /// </summary>
    public void Play()
    {
        StartCoroutine(SpawnSparkles());
    }

    IEnumerator SpawnSparkles()
    {
        if (canvasRect == null)
        {
            Debug.LogWarning("[LevelCompleteEffect] Canvas не найден! Эффект не запущен.");
            yield break;
        }

        float canvasWidth = canvasRect.rect.width;
        float canvasHeight = canvasRect.rect.height;

        // Спавним частицы волнами
        int waves = 3;
        int perWave = particlesPerSide / waves;

        for (int w = 0; w < waves; w++)
        {
            // Левая сторона
            for (int i = 0; i < perWave; i++)
            {
                SpawnParticle(
                    new Vector2(-canvasWidth * 0.5f - 50f, Random.Range(-canvasHeight * 0.4f, canvasHeight * 0.4f)),
                    new Vector2(Random.Range(0.5f, 1f), Random.Range(-0.5f, 0.5f)).normalized,
                    canvasWidth, canvasHeight
                );
            }

            // Правая сторона
            for (int i = 0; i < perWave; i++)
            {
                SpawnParticle(
                    new Vector2(canvasWidth * 0.5f + 50f, Random.Range(-canvasHeight * 0.4f, canvasHeight * 0.4f)),
                    new Vector2(Random.Range(-1f, -0.5f), Random.Range(-0.5f, 0.5f)).normalized,
                    canvasWidth, canvasHeight
                );
            }

            yield return new WaitForSeconds(duration / waves * 0.3f);
        }
    }

    void SpawnParticle(Vector2 startPos, Vector2 direction, float canvasW, float canvasH)
    {
        // Создаём UI-объект
        GameObject particle = new GameObject("Sparkle");
        particle.transform.SetParent(transform, false);

        RectTransform rt = particle.AddComponent<RectTransform>();
        rt.anchoredPosition = startPos;
        
        float size = Random.Range(minSize, maxSize);
        rt.sizeDelta = new Vector2(size, size);

        // Добавляем Image (квадрат/звёздочка)
        Image img = particle.AddComponent<Image>();
        img.color = sparkleColors[Random.Range(0, sparkleColors.Length)];
        img.raycastTarget = false;

        // Запускаем анимацию полёта
        StartCoroutine(AnimateParticle(rt, img, direction, size));
    }

    IEnumerator AnimateParticle(RectTransform rt, Image img, Vector2 direction, float size)
    {
        float lifetime = Random.Range(duration * 0.5f, duration);
        float elapsed = 0f;
        
        float actualSpeed = speed * Random.Range(0.4f, 1.2f);
        float rotSpeed = Random.Range(-360f, 360f);
        float gravity = Random.Range(50f, 200f); // Падение вниз

        // Начальный угол
        float angle = Random.Range(0f, 360f);
        
        // Начальная задержка (для волнового эффекта)
        float delay = Random.Range(0f, 0.3f);
        yield return new WaitForSeconds(delay);

        // Пульсация размера
        float pulseSpeed = Random.Range(3f, 8f);
        float pulseAmount = Random.Range(0.3f, 0.6f);

        while (elapsed < lifetime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / lifetime;

            // Движение
            Vector2 pos = rt.anchoredPosition;
            pos += direction * actualSpeed * Time.deltaTime;
            pos.y -= gravity * t * Time.deltaTime; // Гравитация увеличивается со временем
            rt.anchoredPosition = pos;

            // Вращение
            angle += rotSpeed * Time.deltaTime;
            rt.localRotation = Quaternion.Euler(0, 0, angle);

            // Пульсация размера
            float pulse = 1f + Mathf.Sin(elapsed * pulseSpeed) * pulseAmount;
            rt.sizeDelta = new Vector2(size * pulse, size * pulse);

            // Затухание (fade out в конце)
            float alpha = 1f;
            if (t > 0.6f)
                alpha = Mathf.Lerp(1f, 0f, (t - 0.6f) / 0.4f);
            
            Color c = img.color;
            c.a = alpha;
            img.color = c;

            yield return null;
        }

        // Удаляем частицу
        Destroy(rt.gameObject);
    }
}
