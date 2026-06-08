using UnityEngine;
using UnityEngine.Networking;
using TMPro;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

// ── Существующие модели ──────────────────────────────────────────────────────
[System.Serializable]
public class ScenarioDBItem
{
    public int    id;
    public string scenario_name;
    public string json_data;
    public int    likes;
    public int    views;
    public bool   isLiked;
    // Заполняется локально после запроса к scenario_history.php
    [System.NonSerialized] public bool   isCompleted;
    [System.NonSerialized] public int    completedCount;
}

[System.Serializable]
public class ScenarioDBResult
{
    public List<ScenarioDBItem> items;
}

// ── Модели истории ────────────────────────────────────────────────────────────
[System.Serializable]
public class HistoryEntry
{
    public int    id;
    public int    scenario_id;
    public string scenario_name;
    public string completed_at;
    public int    steps_total;
    public int    steps_done;
}

[System.Serializable]
public class HistoryResult
{
    public bool               success;
    public List<HistoryEntry> history;
}

[System.Serializable]
public class CompletedCheckResult
{
    public bool success;
    public bool completed;
    public int  count;
}

// ── Контроллер ───────────────────────────────────────────────────────────────
public class ScenarioListController : MonoBehaviour
{
    private const string BASE_URL = "https://autoreduce.kz/";

    [Header("UI — Список сценариев")]
    public Transform   contentPanel;
    public GameObject  buttonPrefab;
    public GameObject  loadingText;

    [Header("UI — Панель Истории")]
    public GameObject  historyPanel;          // Панель поверх экрана
    public Transform   historyContent;        // ScrollView Content внутри панели
    public GameObject  historyEntryPrefab;    // Префаб одной строки истории
    public Button      closeHistoryButton;    // Кнопка X / Закрыть
    public Button      openHistoryButton;     // Кнопка "История" (в шапке экрана)
    public TextMeshProUGUI historyTitle;      // "История прохождений"

    [Header("Like Settings")]
    public Sprite likedSprite;
    public Sprite notLikedSprite;

    [Header("Completed Badge")]
    public Sprite completedSprite;            // Зелёная галочка / звёздочка

    // ── Lifecycle ──────────────────────────────────────────────────────────────
    void Start()
    {
        // Прячем панель истории при старте
        if (historyPanel != null) historyPanel.SetActive(false);

        if (closeHistoryButton != null)
            closeHistoryButton.onClick.AddListener(() => historyPanel.SetActive(false));

        if (openHistoryButton != null)
            openHistoryButton.onClick.AddListener(OnOpenHistoryClicked);

        // Если пользователь вернулся из AR после прохождения — записываем историю
        CheckAndRecordPendingHistory();

        // Автоматически заполняем ScrollView историей — если он привязан в Инспекторе
        if (historyContent != null && historyEntryPrefab != null)
            StartCoroutine(FetchAndShowHistory(0, ""));

        StartCoroutine(FetchScenarios());
    }

    /// <summary>
    /// Проверяет, остался ли незаписанный результат прохождения (ScenarioCompleted=1).
    /// ScenarioController ничего не знает об этом — мы сами ставим флаг при запуске,
    /// а проверяем здесь при возврате на экран списка.
    /// </summary>
    private void CheckAndRecordPendingHistory()
    {
        int    pendingId   = PlayerPrefs.GetInt("PendingHistoryScenarioID", 0);
        int    stepsTotal  = PlayerPrefs.GetInt("PendingHistoryStepsTotal", 0);
        string pendingName = PlayerPrefs.GetString("PendingHistoryScenarioName", "");

        if (pendingId > 0 && stepsTotal > 0)
        {
            StartCoroutine(RecordCompletion(pendingId, pendingName, stepsTotal, stepsTotal));
        }

        PlayerPrefs.DeleteKey("PendingHistoryScenarioID");
        PlayerPrefs.DeleteKey("PendingHistoryStepsTotal");
        PlayerPrefs.DeleteKey("PendingHistoryScenarioName");
        PlayerPrefs.Save();
    }

    // ── Загрузка списка ────────────────────────────────────────────────────────
    private IEnumerator FetchScenarios()
    {
        if (loadingText != null) loadingText.SetActive(true);

        int    userId = PlayerPrefs.GetInt("CurrentUserID", 1);
        string url    = BASE_URL + "get_scenarios.php?user_id=" + userId;

        using (UnityWebRequest www = UnityWebRequest.Get(url))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                ScenarioDBResult result = JsonUtility.FromJson<ScenarioDBResult>(www.downloadHandler.text);

                // Для каждого сценария проверяем, проходил ли пользователь его
                yield return StartCoroutine(FetchCompletedFlags(result.items, userId));

                PopulateList(result.items);
            }
            else
            {
                Debug.LogError("Ошибка загрузки сценариев: " + www.error);
            }
        }

        if (loadingText != null) loadingText.SetActive(false);
    }

    /// <summary>
    /// Для каждого сценария делает один запрос ?action=check и заполняет isCompleted.
    /// </summary>
    private IEnumerator FetchCompletedFlags(List<ScenarioDBItem> items, int userId)
    {
        foreach (ScenarioDBItem sc in items)
        {
            string url = $"{BASE_URL}scenario_history.php?action=check&user_id={userId}&scenario_id={sc.id}";
            using (UnityWebRequest www = UnityWebRequest.Get(url))
            {
                yield return www.SendWebRequest();
                if (www.result == UnityWebRequest.Result.Success)
                {
                    CompletedCheckResult check =
                        JsonUtility.FromJson<CompletedCheckResult>(www.downloadHandler.text);
                    sc.isCompleted    = check.completed;
                    sc.completedCount = check.count;
                }
            }
        }
    }

    // ── Рендер карточек ────────────────────────────────────────────────────────
    private void PopulateList(List<ScenarioDBItem> scenarios)
    {
        foreach (Transform child in contentPanel)
            Destroy(child.gameObject);

        foreach (ScenarioDBItem sc in scenarios)
        {
            GameObject newBtnObj = Instantiate(buttonPrefab, contentPanel);

            // 1. Название
            TextMeshProUGUI btnText = newBtnObj.GetComponentInChildren<TextMeshProUGUI>();
            if (btnText != null)
                btnText.text = sc.scenario_name;

            // 2. Значок "пройдено" (ищем объект CompletedBadge в префабе)
            Image completedBadge = newBtnObj.transform.Find("CompletedBadge")?.GetComponent<Image>();
            if (completedBadge != null)
            {
                completedBadge.gameObject.SetActive(sc.isCompleted);
                if (sc.isCompleted && completedSprite != null)
                    completedBadge.sprite = completedSprite;
            }

            // Текст "пройдено N раз" (опционально — объект CompletedCountText)
            TextMeshProUGUI completedCountTxt =
                newBtnObj.transform.Find("CompletedCountText")?.GetComponent<TextMeshProUGUI>();
            if (completedCountTxt != null)
            {
                completedCountTxt.gameObject.SetActive(sc.isCompleted);
                if (sc.isCompleted)
                    completedCountTxt.text = $"✓ {sc.completedCount}x";
            }

            // 3. Лайк
            Button        likeBtn       = newBtnObj.transform.Find("LikeButton")?.GetComponent<Button>();
            Image         likeIcon      = newBtnObj.transform.Find("LikeButton/Like")?.GetComponent<Image>();
            TextMeshProUGUI likeCountTxt = newBtnObj.transform.Find("LikeCount")?.GetComponent<TextMeshProUGUI>();

            if (likeBtn != null)
            {
                if (likeCountTxt != null) likeCountTxt.text = sc.likes.ToString();
                if (likeIcon    != null) likeIcon.sprite    = sc.isLiked ? likedSprite : notLikedSprite;

                bool currentLiked = sc.isLiked;
                int  scenarioId   = sc.id;

                likeBtn.onClick.AddListener(() =>
                {
                    currentLiked = !currentLiked;
                    if (likeCountTxt != null)
                    {
                        int val = int.Parse(likeCountTxt.text);
                        likeCountTxt.text = (currentLiked ? val + 1 : val - 1).ToString();
                    }
                    if (likeIcon != null)
                        likeIcon.sprite = currentLiked ? likedSprite : notLikedSprite;

                    StartCoroutine(SendLikeRequest(scenarioId, currentLiked));
                });
            }

            // 4. Основная кнопка — запуск AR
            Button mainBtn = newBtnObj.GetComponent<Button>();
            mainBtn.onClick.AddListener(() => OnScenarioButtonClicked(sc.id, sc.json_data));

            // 5. Кнопка Info / Комментарии
            Button infoBtn = newBtnObj.transform.Find("InfoButton")?.GetComponent<Button>();
            if (infoBtn != null)
                infoBtn.onClick.AddListener(() => OnCommentsButtonClicked(sc.id, sc.scenario_name));

            // 6. Кнопка "История этого сценария"
            Button histBtn = newBtnObj.transform.Find("HistoryButton")?.GetComponent<Button>();
            if (histBtn != null)
            {
                int capturedId     = sc.id;
                string capturedName = sc.scenario_name;
                histBtn.onClick.AddListener(() => OnOpenScenarioHistoryClicked(capturedId, capturedName));
            }
        }
    }

    // ── Кнопка "Вся история пользователя" (из шапки) ──────────────────────────
    private void OnOpenHistoryClicked()
    {
        if (historyPanel == null) return;
        historyPanel.SetActive(true);
        if (historyTitle != null) historyTitle.text = "Барлық тарих / Вся история";
        StartCoroutine(FetchAndShowHistory(0, ""));
    }

    // ── История конкретного сценария ──────────────────────────────────────────
    private void OnOpenScenarioHistoryClicked(int scenarioId, string scenarioName)
    {
        if (historyPanel == null) return;
        historyPanel.SetActive(true);
        if (historyTitle != null) historyTitle.text = $"Тарих: {scenarioName}";
        StartCoroutine(FetchAndShowHistory(scenarioId, scenarioName));
    }

    /// <summary>
    /// Загружает историю и заполняет панель.
    /// scenarioId == 0  →  вся история пользователя
    /// scenarioId  > 0  →  история только для этого сценария
    /// </summary>
    private IEnumerator FetchAndShowHistory(int scenarioId, string scenarioName)
    {
        // Очищаем старые строки
        foreach (Transform child in historyContent)
            Destroy(child.gameObject);

        int    userId = PlayerPrefs.GetInt("CurrentUserID", 1);
        string url    = $"{BASE_URL}scenario_history.php?action=get&user_id={userId}";

        using (UnityWebRequest www = UnityWebRequest.Get(url))
        {
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("История: ошибка сети — " + www.error);
                yield break;
            }

            HistoryResult data = JsonUtility.FromJson<HistoryResult>(www.downloadHandler.text);
            if (data == null || data.history == null || data.history.Count == 0)
            {
                SpawnNoHistoryRow();
                yield break;
            }

            foreach (HistoryEntry entry in data.history)
            {
                // Фильтруем, если нужен конкретный сценарий
                if (scenarioId > 0 && entry.scenario_id != scenarioId) continue;
                SpawnHistoryRow(entry);
            }
        }
    }

    private void SpawnNoHistoryRow()
    {
        if (historyEntryPrefab == null || historyContent == null) return;
        GameObject row = Instantiate(historyEntryPrefab, historyContent);
        TextMeshProUGUI[] texts = row.GetComponentsInChildren<TextMeshProUGUI>();
        if (texts.Length > 0) texts[0].text = "Тарих жоқ / Нет истории";
    }

    private void SpawnHistoryRow(HistoryEntry entry)
    {
        if (historyEntryPrefab == null || historyContent == null) return;

        GameObject row = Instantiate(historyEntryPrefab, historyContent);

        // Ищем названные дочерние TextMeshPro
        TextMeshProUGUI nameTxt  = row.transform.Find("HistoryNameText")?.GetComponent<TextMeshProUGUI>();
        TextMeshProUGUI dateTxt  = row.transform.Find("HistoryDateText")?.GetComponent<TextMeshProUGUI>();
        TextMeshProUGUI stepsTxt = row.transform.Find("HistoryStepsText")?.GetComponent<TextMeshProUGUI>();

        // Если префаб простой (один TMP) — записываем всё в него
        if (nameTxt == null && dateTxt == null)
        {
            TextMeshProUGUI single = row.GetComponentInChildren<TextMeshProUGUI>();
            if (single != null)
            {
                string stepsInfo = entry.steps_total > 0
                    ? $"  ({entry.steps_done}/{entry.steps_total} қадам)"
                    : "";
                single.text = $"{entry.scenario_name}\n" +
                              $"<size=70%><color=#888888>{FormatDate(entry.completed_at)}{stepsInfo}</color></size>";
            }
            return;
        }

        // Названные дочерние объекты
        if (nameTxt  != null) nameTxt.text  = entry.scenario_name;
        if (dateTxt  != null) dateTxt.text  = FormatDate(entry.completed_at);
        if (stepsTxt != null)
            stepsTxt.text = entry.steps_total > 0
                ? $"{entry.steps_done}/{entry.steps_total} қадам"
                : "";

        // Опционально: зеленая полоска слева
        Image badgeImg = row.transform.Find("CompletedBadge")?.GetComponent<Image>();
        if (badgeImg != null)
        {
            badgeImg.gameObject.SetActive(true);
            if (completedSprite != null) badgeImg.sprite = completedSprite;
        }
    }

    private string FormatDate(string rawDate)
    {
        if (System.DateTime.TryParse(rawDate, out System.DateTime dt))
            return dt.ToString("dd.MM.yyyy  HH:mm");
        return rawDate;
    }

    // ── Запись прохождения (вызывается из ScenarioController при завершении) ──
    public static IEnumerator RecordCompletion(int scenarioId, string scenarioName, int stepsTotal, int stepsDone)
    {
        int userId = PlayerPrefs.GetInt("CurrentUserID", 1);
        if (userId == 0) yield break;

        WWWForm form = new WWWForm();
        form.AddField("user_id",       userId);
        form.AddField("scenario_id",   scenarioId);
        form.AddField("scenario_name", scenarioName);
        form.AddField("steps_total",   stepsTotal);
        form.AddField("steps_done",    stepsDone);

        using (UnityWebRequest www = UnityWebRequest.Post(
            "https://autoreduce.kz/scenario_history.php", form))
        {
            yield return www.SendWebRequest();
            if (www.result == UnityWebRequest.Result.Success)
                Debug.Log("История записана: " + www.downloadHandler.text);
            else
                Debug.LogWarning("История — ошибка: " + www.error);
        }
    }

    // ── Лайк ──────────────────────────────────────────────────────────────────
    private IEnumerator SendLikeRequest(int scenarioId, bool isLiking)
    {
        int userId = PlayerPrefs.GetInt("CurrentUserID", 1);
        if (userId == 0) yield break;

        WWWForm form = new WWWForm();
        form.AddField("user_id",     userId);
        form.AddField("scenario_id", scenarioId);
        form.AddField("action",      isLiking ? "like" : "unlike");

        using (UnityWebRequest www = UnityWebRequest.Post(BASE_URL + "like_logic.php", form))
        {
            yield return www.SendWebRequest();
            if (www.result != UnityWebRequest.Result.Success)
                Debug.LogError("Лайк — ошибка: " + www.error);
            else
                Debug.Log("Лайк — ответ: " + www.downloadHandler.text);
        }
    }

    // ── Навигация ──────────────────────────────────────────────────────────────
    private void OnCommentsButtonClicked(int scenarioId, string scenarioName)
    {
        PlayerPrefs.SetInt("SelectedScenarioID",     scenarioId);
        PlayerPrefs.SetString("SelectedScenarioName", scenarioName);
        SceneManager.LoadScene("CommentScene");
    }

    private void OnScenarioButtonClicked(int scenarioId, string jsonData)
    {
        CustomScenario loadedScenario = JsonUtility.FromJson<CustomScenario>(jsonData);
        ScenarioManager.GetInstance().SelectCustomScenario(loadedScenario);

        // Ставим флаги: когда пользователь ВЕРНЁТСЯ на этот экран,
        // CheckAndRecordPendingHistory() увидит их и запишет историю.
        // stepsTotal = 0 означает "неизвестно" — ScenarioController сам знает.
        // Здесь достаточно signaling: ID > 0, stepsTotal = 1 (placeholder).
        PlayerPrefs.SetInt("PendingHistoryScenarioID",    scenarioId);
        PlayerPrefs.SetInt("PendingHistoryStepsTotal",     1);
        PlayerPrefs.SetString("PendingHistoryScenarioName", loadedScenario?.scenarioName ?? "");
        PlayerPrefs.Save();

        StartCoroutine(UpdateViewAndLoadScene(scenarioId));
    }

    private IEnumerator UpdateViewAndLoadScene(int id)
    {
        WWWForm form = new WWWForm();
        form.AddField("scenario_id", id);

        using (UnityWebRequest www = UnityWebRequest.Post(BASE_URL + "view_scenario.php", form))
        {
            yield return www.SendWebRequest();
            if (www.result != UnityWebRequest.Result.Success)
                Debug.LogWarning("Просмотры — ошибка: " + www.error);
        }

        ARSceneLoadGuard.LoadARScene();
    }
}