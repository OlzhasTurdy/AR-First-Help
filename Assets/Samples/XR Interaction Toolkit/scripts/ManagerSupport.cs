using UnityEngine;

public class SupportManager : MonoBehaviour
{
    [Header("Базовая ссылка на чат поддержки")]
    public string baseUrl = "https://autoreduce.kz/user_chat.php";

    // Переменная для хранения email или ID текущего пользователя
    // Позже вы можете перезаписывать её из скрипта авторизации
    [Header("Данные пользователя")]
    public string currentUserEmail = "olzhas@example.com";

    /// <summary>
    /// Этот метод нужно назначить на событие OnClick вашей кнопки в UI
    /// </summary>
    public void OpenSupportWebpage()
    {
        // Формируем правильную ссылку с параметром ?email=...
        // Если вы поменяли PHP, чтобы он принимал id, замените "?email=" на "?id="
        string finalUrl = baseUrl + "?email=" + currentUserEmail;

        // Выводим в консоль для проверки
        Debug.Log("Открываем поддержку по ссылке: " + finalUrl);

        // Открываем браузер на устройстве
        Application.OpenURL(finalUrl);
    }

    /// <summary>
    /// Метод для вызова из ДРУГИХ скриптов (когда ID передается динамически)
    /// </summary>
    /// <param name="userId">ID или Email пользователя</param>
    public void OpenSupportWithID(string userId)
    {
        string finalUrl = baseUrl + "?id=" + userId;
        Application.OpenURL(finalUrl);
    }
}