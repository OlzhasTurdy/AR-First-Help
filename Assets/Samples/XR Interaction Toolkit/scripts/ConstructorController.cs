using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using System.Collections;

public class ConstructorController : MonoBehaviour
{
    [Header("Input Fields")]
    public TMP_InputField stepTitleInput;
    public TMP_InputField descriptionInput;
    public TMP_InputField warningsInput;
    public TMP_InputField modelUrlInput;
    public TMP_InputField videoUrlInput;  // новое — привязать в Inspector
    public TMP_InputField quizInput;      // новое — привязать в Inspector

    public void OnNextStepClicked()
    {
        if (!ValidateFields()) return;

        CustomStep newStep = new CustomStep
        {
            title = stepTitleInput.text,
            description = descriptionInput.text,
            warnings = warningsInput.text,
            modelUrl = modelUrlInput.text,
            videoUrl = videoUrlInput.text.Trim(),
            quizRaw = quizInput.text.Trim()
        };

        ScenarioDraft.CurrentDraft.steps.Add(newStep);
        Debug.Log("Шаг добавлен! Всего шагов: " + ScenarioDraft.CurrentDraft.steps.Count);
        ClearFields();
    }

    public void OnSaveScenarioClicked()
    {
        if (!string.IsNullOrWhiteSpace(stepTitleInput.text))
            OnNextStepClicked();

        if (ScenarioDraft.CurrentDraft.steps.Count == 0)
        {
            Debug.LogWarning("Сценарий не может быть пустым!");
            return;
        }

        string jsonOutput = JsonUtility.ToJson(ScenarioDraft.CurrentDraft, true);
        Debug.Log("ГОТОВЫЙ JSON ДЛЯ СЕРВЕРА:\n" + jsonOutput);
        StartCoroutine(SendScenarioToServer(jsonOutput));
    }

    private IEnumerator SendScenarioToServer(string json)
    {
        string url = "https://autoreduce.kz/save_scenario.php";

        WWWForm form = new WWWForm();
        form.AddField("scenario_json", json);
        form.AddField("user_id", PlayerPrefs.GetInt("userId", 1).ToString());

        using (UnityWebRequest www = UnityWebRequest.Post(url, form))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("Успешно сохранено: " + www.downloadHandler.text);
                ScenarioDraft.CurrentDraft = new CustomScenario();
                yield return new WaitForSeconds(2f);
                SceneManager.LoadScene("Untitled");
            }
            else
            {
                Debug.LogError("Ошибка отправки: " + www.error);
            }
        }
    }

    private bool ValidateFields()
    {
        // modelUrl, videoUrl, quizRaw — необязательные поля, не валидируем
        if (string.IsNullOrWhiteSpace(stepTitleInput.text) ||
            string.IsNullOrWhiteSpace(descriptionInput.text) ||
            string.IsNullOrWhiteSpace(warningsInput.text))
        {
            Debug.LogWarning("Заполните обязательные поля: заголовок, описание, предупреждения!");
            return false;
        }
        return true;
    }

    private void ClearFields()
    {
        stepTitleInput.text = "";
        descriptionInput.text = "";
        warningsInput.text = "";
        modelUrlInput.text = "";
        videoUrlInput.text = "";
        quizInput.text = "";
        stepTitleInput.Select();
    }
}