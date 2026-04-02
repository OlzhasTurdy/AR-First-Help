using UnityEngine;

public class TogglePanel : MonoBehaviour
{
    // Слово 'public' обязательно, чтобы поле появилось в инспекторе
    public GameObject panel;

    public void Toggle()
    {
        if (panel != null)
        {
            panel.SetActive(!panel.activeSelf);
        }
    }
}