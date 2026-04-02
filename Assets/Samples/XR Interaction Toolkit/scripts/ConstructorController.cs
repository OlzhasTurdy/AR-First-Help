using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.Networking; // ОБЯЗАТЕЛЬНО ДОБАВИТЬ для запросов
using System.Collections;     // ОБЯЗАТЕЛЬНО ДОБАВИТЬ для корутин

public class ConstructorController : MonoBehaviour
{
    [Header("Input Fields")]
    public TMP_InputField stepTitleInput;
    public TMP_InputField descriptionInput;
    public TMP_InputField warningsInput;
    public TMP_InputField modelUrlInput;

    // Метод для кнопки "Next Step"
    public void OnNextStepClicked()
    {
        if (!ValidateFields()) return; // Если поля пустые, прерываем

        // 1. Создаем новый шаг из данных в полях
        CustomStep newStep = new CustomStep
        {
            title = stepTitleInput.text,
            description = descriptionInput.text,
            warnings = warningsInput.text,
            modelUrl = modelUrlInput.text
        };

        // 2. Добавляем его в наш глобальный черновик
        ScenarioDraft.CurrentDraft.steps.Add(newStep);
        Debug.Log("Шаг добавлен! Всего шагов: " + ScenarioDraft.CurrentDraft.steps.Count);

        // 3. Очищаем поля для ввода следующего шага
        ClearFields();
    }

    // Метод для кнопки "Save Scenario"
    public void OnSaveScenarioClicked()
    {
        // Если пользователь заполнил поля, но забыл нажать Next, 
        // мы можем сохранить текущие данные как последний шаг
        if (!string.IsNullOrWhiteSpace(stepTitleInput.text))
        {
            OnNextStepClicked();
        }

        // Проверяем, есть ли вообще шаги
        if (ScenarioDraft.CurrentDraft.steps.Count == 0)
        {
            Debug.LogWarning("Сценарий не может быть пустым!");
            return;
        }

        // --- ФИНАЛ: ПРЕВРАЩАЕМ В JSON ---
        string jsonOutput = JsonUtility.ToJson(ScenarioDraft.CurrentDraft, true);
        Debug.Log("ГОТОВЫЙ JSON ДЛЯ СЕРВЕРА:\n" + jsonOutput);

        StartCoroutine(SendScenarioToServer(jsonOutput));

        // После сохранения возвращаемся в главное меню
        
    }
    private IEnumerator SendScenarioToServer(string json)
    {
        // Укажите ВАШ реальный URL к PHP скрипту
        string url = "https://autoreduce.kz/save_scenario.php";

        // Создаем форму и кладем туда наш JSON
        WWWForm form = new WWWForm();
        form.AddField("scenario_json", json);

        // --- ИСПРАВЛЕНИЕ: ДОБАВЛЯЕМ user_id ---
        // Берем ID текущего авторизованного пользователя. 
        // (Для теста стоит 1, но в релизе берите реальный ID из системы логина)
        int currentUserId = PlayerPrefs.GetInt("userId", 1);
        form.AddField("user_id", currentUserId.ToString());
        // --------------------------------------

        // Отправляем POST запрос
        using (UnityWebRequest www = UnityWebRequest.Post(url, form))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("Успешно сохранено на сервере: " + www.downloadHandler.text);

                // Сбрасываем черновик, чтобы он был чистым для следующих сценариев
                ScenarioDraft.CurrentDraft = new CustomScenario();

                yield return new WaitForSeconds(2f);

                // 3. ПЕРЕХОДИМ НА ГЛАВНУЮ СЦЕНУ
                SceneManager.LoadScene("Untitled");
            }
            else
            {
                Debug.LogError("Ошибка отправки: " + www.error);
            }
        }
    }

    // Вспомогательный метод проверки полей
    private bool ValidateFields()
    {
        if (string.IsNullOrWhiteSpace(stepTitleInput.text) ||
            string.IsNullOrWhiteSpace(descriptionInput.text) ||
            string.IsNullOrWhiteSpace(warningsInput.text) ||
            string.IsNullOrWhiteSpace(modelUrlInput.text))
        {
            Debug.LogWarning("Пожалуйста, заполните все поля!");
            return false;
        }
        return true;
    }

    // Вспомогательный метод очистки UI
    private void ClearFields()
    {
        stepTitleInput.text = "";
        descriptionInput.text = "";
        warningsInput.text = "";
        modelUrlInput.text = "";

        // Возвращаем фокус на первое поле (опционально)
        stepTitleInput.Select();
    }
}