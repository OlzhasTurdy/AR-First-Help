using UnityEngine;
using UnityEngine.SceneManagement;

// Перечисление типов (теперь это глобальный тип данных)
public enum EmergencyType { None, CPR, Bleeding, Choking, Unconscious }

public class EmergencyLoader : MonoBehaviour
{
    // Храним выбор в формате EmergencyType, а не в строке
    public static EmergencyType SelectedEmergency = EmergencyType.None;

    // Метод для кнопок
    public void LoadEmergency(string typeName)
    {
        // Превращаем строку из кнопки в значение Enum
        if (System.Enum.TryParse(typeName, out EmergencyType result))
        {
            SelectedEmergency = result;
            SceneManager.LoadScene("Emergency"); // Укажите имя вашей сцены
        }
        else
        {
            Debug.LogError("Ошибка: Тип '" + typeName + "' не найден в EmergencyType!");
        }
    }
}