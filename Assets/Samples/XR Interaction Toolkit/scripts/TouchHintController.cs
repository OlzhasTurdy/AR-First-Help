using UnityEngine;
using UnityEngine.EventSystems;

public class TouchHintController : MonoBehaviour
{
    [Header("Панель с указателем")]
    public GameObject touchPanel; // перетащи сюда touchpanel из Hierarchy

    [Header("Задержка появления (сек)")]
    public float showDelay = 1.4f;

    private float _timer = 0f;
    private bool _panelShown = false;
    private bool _touched = false;

    void Start()
    {
        if (touchPanel != null)
            touchPanel.SetActive(false);
    }

    void Update()
    {
        // Проверяем касание (работает и на мобильном, и в редакторе)
        bool isTouching = Input.touchCount > 0 || Input.GetMouseButtonDown(0);

        if (isTouching)
        {
            _touched = true;

            // Если панель уже показана — скрываем
            if (_panelShown)
            {
                HidePanel();
            }
        }

        // Таймер: считаем только если касания ещё не было
        if (!_touched && !_panelShown)
        {
            _timer += Time.deltaTime;

            if (_timer >= showDelay)
            {
                ShowPanel();
            }
        }
    }

    void ShowPanel()
    {
        if (touchPanel != null)
            touchPanel.SetActive(true);
        _panelShown = true;
    }

    void HidePanel()
    {
        if (touchPanel != null)
            touchPanel.SetActive(false);
        _panelShown = false;
    }
}
