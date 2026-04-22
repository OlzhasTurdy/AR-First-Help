using UnityEngine;
using UnityEngine.Networking;
using TMPro;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

[System.Serializable]
public class ScenarioDBItem
{
    public int id;
    public string scenario_name;
    public string json_data;
    public int likes;
    public int views;
    public bool isLiked; // Приходит от PHP (true/false)
}

[System.Serializable]
public class ScenarioDBResult
{
    public List<ScenarioDBItem> items;
}

public class ScenarioListController : MonoBehaviour
{
    [Header("UI Elements")]
    public Transform contentPanel;
    public GameObject buttonPrefab;
    public GameObject loadingText;

    [Header("Like Settings")]
    public Sprite likedSprite;      // Красное/Заполненное сердечко
    public Sprite notLikedSprite;   // Пустое/Серое сердечко

    void Start()
    {
        StartCoroutine(FetchScenarios());
    }

    private IEnumerator FetchScenarios()
    {
        if (loadingText != null) loadingText.SetActive(true);

        // Получаем ID игрока (должен быть сохранен при логине)
        int userId = PlayerPrefs.GetInt("CurrentUserID", 1);
        Debug.Log("Загружаем сценарии для пользователя с ID: " + userId);
        string url = "https://autoreduce.kz/get_scenarios.php?user_id=" + userId;

        using (UnityWebRequest www = UnityWebRequest.Get(url))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                string jsonResponse = www.downloadHandler.text;
                ScenarioDBResult result = JsonUtility.FromJson<ScenarioDBResult>(jsonResponse);
                PopulateList(result.items);
            }
            else
            {
                Debug.LogError("Ошибка загрузки: " + www.error);
            }
        }

        if (loadingText != null) loadingText.SetActive(false);
    }

    private void PopulateList(List<ScenarioDBItem> scenarios)
    {
        foreach (Transform child in contentPanel)
        {
            Destroy(child.gameObject);
        }

        foreach (ScenarioDBItem sc in scenarios)
        {
            GameObject newBtnObj = Instantiate(buttonPrefab, contentPanel);

            // 1. Имя и просмотры
            TextMeshProUGUI btnText = newBtnObj.GetComponentInChildren<TextMeshProUGUI>();
            if (btnText != null)
                btnText.text = $"{sc.scenario_name}";

            // 2. Логика Лайка
            // Ищем кнопку лайка по имени в префабе (например "LikeButton")
            Button likeBtn = newBtnObj.transform.Find("LikeButton")?.GetComponent<Button>();
            Image likeIcon = newBtnObj.transform.Find("LikeButton/Like")?.GetComponent<Image>();
            TextMeshProUGUI likeCountText = newBtnObj.transform.Find("LikeCount")?.GetComponent<TextMeshProUGUI>();

            if (likeBtn != null)
            {
                // Устанавливаем начальное состояние из БД
                if (likeCountText != null) likeCountText.text = sc.likes.ToString();
                if (likeIcon != null) likeIcon.sprite = sc.isLiked ? likedSprite : notLikedSprite;

                bool currentStatus = sc.isLiked;
                int scenarioId = sc.id;

                likeBtn.onClick.AddListener(() => {
                    // Переключаем локально для скорости
                    currentStatus = !currentStatus;

                    // Обновляем текст (визуально прибавляем/отнимаем 1)
                    if (likeCountText != null)
                    {
                        int val = int.Parse(likeCountText.text);
                        val = currentStatus ? val + 1 : val - 1;
                        likeCountText.text = val.ToString();
                    }

                    // Меняем иконку
                    if (likeIcon != null) likeIcon.sprite = currentStatus ? likedSprite : notLikedSprite;

                    // Отправляем на сервер
                    // Передаем currentStatus, чтобы сервер знал, ставим мы лайк или убираем
                    StartCoroutine(SendLikeRequest(scenarioId, currentStatus));
                });
            }

            // 3. Логика основной кнопки (Запуск AR)
            Button mainBtn = newBtnObj.GetComponent<Button>();
            mainBtn.onClick.AddListener(() => OnScenarioButtonClicked(sc.id, sc.json_data));

            // 4. Логика кнопки Info
            Button infoBtn = newBtnObj.transform.Find("InfoButton")?.GetComponent<Button>();
            if (infoBtn != null)
            {
                infoBtn.onClick.AddListener(() => OnCommentsButtonClicked(sc.id, sc.scenario_name));
            }
        }
    }

    private IEnumerator SendLikeRequest(int scenarioId, bool isLiking)
    {
        int userId = PlayerPrefs.GetInt("CurrentUserID", 1);
        if (userId == 0) yield break;

        WWWForm form = new WWWForm();
        form.AddField("user_id", userId);
        form.AddField("scenario_id", scenarioId);

        // ДОБАВЛЕНО: отправляем параметр action, который ждет PHP
        form.AddField("action", isLiking ? "like" : "unlike");

        // ВНИМАНИЕ: Убедись, что имя файла здесь правильное! 
        // У тебя в одном месте был like_scenario.php, в другом like_logic.php
        string url = "https://autoreduce.kz/like_logic.php";

        using (UnityWebRequest www = UnityWebRequest.Post(url, form))
        {
            yield return www.SendWebRequest();

            // Добавим логирование для удобной отладки
            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Ошибка запроса: " + www.error);
            }
            else
            {
                Debug.Log("Ответ сервера: " + www.downloadHandler.text);
            }
        }
    }

    private void OnCommentsButtonClicked(int scenarioId, string scenarioName)
    {
        PlayerPrefs.SetInt("SelectedScenarioID", scenarioId);
        PlayerPrefs.SetString("SelectedScenarioName", scenarioName);
        SceneManager.LoadScene("CommentScene");
    }

    private void OnScenarioButtonClicked(int scenarioId, string jsonData)
    {
        // 1. Обязательно передаем данные в наш "бессмертный" менеджер
        CustomScenario loadedScenario = JsonUtility.FromJson<CustomScenario>(jsonData);
        ScenarioManager.GetInstance().SelectCustomScenario(loadedScenario);

        // 2. Запускаем корутину, которая сначала отправит просмотр, а потом загрузит сцену
        StartCoroutine(UpdateViewAndLoadScene(scenarioId));
    }

    IEnumerator UpdateViewCount(int id)
    {
        WWWForm form = new WWWForm();
        form.AddField("scenario_id", id);
        using (UnityWebRequest www = UnityWebRequest.Post("https://autoreduce.kz/view_scenario.php", form))
        {
            yield return www.SendWebRequest();
        }
    }
    private IEnumerator UpdateViewAndLoadScene(int id)
    {
        // Подготавливаем форму
        WWWForm form = new WWWForm();
        form.AddField("scenario_id", id);

        // Отправляем запрос
        using (UnityWebRequest www = UnityWebRequest.Post("https://autoreduce.kz/view_scenario.php", form))
        {
            // yield return заставит Unity подождать, пока запрос не завершится
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning("Не удалось обновить просмотры: " + www.error);
            }
        }

        // ТОЛЬКО ПОСЛЕ ТОГО, как запрос ушел, загружаем AR-сцену
        SceneManager.LoadScene("ARScene");
    }
}