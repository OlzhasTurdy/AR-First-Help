using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class NameScreenController : MonoBehaviour
{
    public TMP_InputField nameInputField;

    public void OnCreateButtonClicked()
    {
        // Проверка: заполнено ли поле?
        if (string.IsNullOrWhiteSpace(nameInputField.text))
        {
            Debug.LogWarning("Введите название сценария!");
            // Тут можно включить красный текст ошибки на UI
            return;
        }

        // 1. Создаем новый чистый черновик
        ScenarioDraft.CurrentDraft = new CustomScenario();

        // 2. Записываем название
        ScenarioDraft.CurrentDraft.scenarioName = nameInputField.text;

        // 3. Загружаем вторую сцену (убедитесь, что она добавлена в Build Settings)
        SceneManager.LoadScene("scenarioConstructor");
    }
}