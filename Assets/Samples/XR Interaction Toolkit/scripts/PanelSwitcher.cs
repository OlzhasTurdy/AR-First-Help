using UnityEngine;

public class PanelSwitcher : MonoBehaviour
{
    [Header("Список твоих панелей (Papa, Papa1 и т.д.)")]
    public GameObject[] panels;

    // Метод, который мы будем вешать на кнопки
    public void ShowPanel(int index)
    {
        for (int i = 0; i < panels.Length; i++)
        {
            // Если индекс совпадает с нажатой кнопкой — включаем, иначе выключаем
            panels[i].SetActive(i == index);
        }
    }
}