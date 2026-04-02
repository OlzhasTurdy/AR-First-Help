using UnityEngine;

public class EmergencyCall : MonoBehaviour
{
    // Универсальный номер службы спасения
    private string phoneNumber = "112";

    // Метод для кнопки
    public void Call112()
    {
        // Открывает стандартное приложение "Телефон" 
        // с уже набранным номером 112
        Application.OpenURL("tel:" + phoneNumber);

        Debug.Log("Переход в приложение телефона: " + phoneNumber);
    }
}