using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AccessControl : MonoBehaviour
{
    public string requiredRole = "root"; // Роль, которой разрешен вход
    private Button myButton;

    void Start()
    {
        myButton = GetComponent<Button>();
        CheckAccess();
    }

    void CheckAccess()
    {
        // Достаем роль текущего пользователя (по умолчанию "guest")
        string currentRole = PlayerPrefs.GetString("user_role", "guest");

        if (currentRole == requiredRole)
        {
            // Если root — кнопка активна и видна
            myButton.interactable = true;
            // Можно добавить визуальный эффект (например, убрать иконку замка)
        }
        else
        {
            // ВАРИАНТ А: Сделать кнопку серой (не нажимаемой)
            myButton.interactable = false;

            // ВАРИАНТ Б: Полностью скрыть кнопку (раскомментируй строку ниже)
            // gameObject.SetActive(false);

            Debug.Log("Доступ ограничен: требуется роль root.");
        }
    }

    // Опционально: метод для вывода уведомления "Нет прав"
    public void ShowDeniedMessage()
    {
        string currentRole = PlayerPrefs.GetString("user_role", "guest");
        if (currentRole != requiredRole)
        {
            // Здесь можно вызвать всплывающее окно (Pop-up)
            Debug.Log("У вас нет прав для создания кастомных сцен!");
        }
    }
}