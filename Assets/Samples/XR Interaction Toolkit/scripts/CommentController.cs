using UnityEngine;
using TMPro;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class FullScenarioData
{
    public ScenarioHeader info;
    public List<CommentItem> comments;
}

[System.Serializable]
public class ScenarioHeader
{
    public string scenario_name;
    public string username;
    public int likes;
    public int views;
}

[System.Serializable]
public class CommentItem
{
    public string username;
    public string comment_text;
}

public class CommentController : MonoBehaviour
{
    [Header("Scenario Info UI")]
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI authorText; // Перетащите сюда объект "Author"
    public TextMeshProUGUI likesText;  // Перетащите сюда объект "Likes"
    public TextMeshProUGUI viewsText;  // Перетащите сюда объект "views"

    [Header("Comments UI")]
    public Transform contentPanel;
    public GameObject commentPrefab;
    public TMP_InputField newCommentInput;

    private int scenarioId;

    void Start()
    {
        // Берем ID из PlayerPrefs, который мы сохранили при клике в списке
        scenarioId = PlayerPrefs.GetInt("SelectedScenarioID", 0);
        StartCoroutine(LoadAllData());
    }

    IEnumerator LoadAllData()
    {
        string url = "https://autoreduce.kz/get_scenario_details.php?id=" + scenarioId;
        using (UnityWebRequest www = UnityWebRequest.Get(url))
        {
            yield return www.SendWebRequest();
            if (www.result == UnityWebRequest.Result.Success)
            {
                FullScenarioData data = JsonUtility.FromJson<FullScenarioData>(www.downloadHandler.text);

                // Заполняем "шапку" данными из БД
                titleText.text = data.info.scenario_name;
                if (authorText) authorText.text = "By: " + data.info.username;
                if (likesText) likesText.text = "Likes: " + data.info.likes;
                if (viewsText) viewsText.text = "Views: " + data.info.views;

                // Очищаем и заполняем список комментариев
                foreach (Transform child in contentPanel) Destroy(child.gameObject);
                foreach (var c in data.comments)
                {
                    GameObject go = Instantiate(commentPrefab, contentPanel);
                    TextMeshProUGUI[] t = go.GetComponentsInChildren<TextMeshProUGUI>();
                    if (t.Length >= 2)
                    {
                        t[0].text = c.username;     // Имя комментатора
                        t[1].text = c.comment_text; // Текст комментария
                    }
                }
            }
        }
    }

    // Метод для Enter в InputField
    // Метод для кнопки (назначьте его в OnClick событиях кнопки)
    public void OnSendButtonClick()
    {
        string text = newCommentInput.text;
        if (!string.IsNullOrWhiteSpace(text))
        {
            StartCoroutine(PostComment(text));
        }
    }

    // Метод для Enter (оставьте его в On End Edit у InputField)
    public void OnSubmitComment(string text)
    {
        // На мобильных OnEndEdit срабатывает часто, поэтому проверяем клавишу
        if (!string.IsNullOrWhiteSpace(text) && (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)))
        {
            StartCoroutine(PostComment(text));
        }
    }

    IEnumerator PostComment(string text)
    {
        // Блокируем ввод, чтобы пользователь не нажал дважды
        newCommentInput.interactable = false;

        WWWForm form = new WWWForm();
        form.AddField("scenario_id", scenarioId);

        // Берем ID текущего пользователя
        int userId = PlayerPrefs.GetInt("logged_in_user_id", 1);
        form.AddField("user_id", userId);
        form.AddField("comment_text", text);

        using (UnityWebRequest www = UnityWebRequest.Post("https://autoreduce.kz/add_comment.php", form))
        {
            yield return www.SendWebRequest();

            newCommentInput.interactable = true;

            if (www.result == UnityWebRequest.Result.Success)
            {
                newCommentInput.text = "";
                StartCoroutine(LoadAllData()); // Обновляем список, чтобы увидеть новый коммент
                Debug.Log("Коммент отправлен: " + www.downloadHandler.text);
            }
            else
            {
                Debug.LogError("Ошибка отправки коммента: " + www.error);
            }
        }
    }
}