using UnityEngine;
using UnityEngine.Networking;
using TMPro;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Отдельный скрипт для экрана Профиля.
/// Прикрепите к любому объекту в ProfileScene.
/// ScenarioListController НЕ НУЖЕН.
/// </summary>
public class ProfileHistoryController : MonoBehaviour
{
    [Header("ScrollView")]
    public Transform  historyContent;       // ScrollView → Viewport → Content
    public GameObject historyEntryPrefab;   // Префаб одной строки

    [Header("Опционально")]
    public TextMeshProUGUI emptyLabel;      // Текст "Нет истории" (если нет записей)
    public GameObject      loadingIndicator;// Спиннер / Loading...

    private const string URL =
        "https://autoreduce.kz/get_user_history.php?user_id=";

    // ──────────────────────────────────────────────────────────────────────────
    void Start() => StartCoroutine(LoadHistory());

    // ──────────────────────────────────────────────────────────────────────────
    private IEnumerator LoadHistory()
    {
        if (loadingIndicator != null) loadingIndicator.SetActive(true);
        if (emptyLabel       != null) emptyLabel.gameObject.SetActive(false);

        int    userId = PlayerPrefs.GetInt("CurrentUserID", 1);
        string url    = URL + userId;

        using (UnityWebRequest www = UnityWebRequest.Get(url))
        {
            yield return www.SendWebRequest();

            if (loadingIndicator != null) loadingIndicator.SetActive(false);

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("ProfileHistory: " + www.error);
                ShowEmpty("Қате / Ошибка загрузки");
                yield break;
            }

            // ── DEBUG: смотрим сырой ответ сервера ──────────────────────────
            Debug.Log("ProfileHistory RAW: " + www.downloadHandler.text);
            // ────────────────────────────────────────────────────────────────

            HistoryResultLocal data =
                JsonUtility.FromJson<HistoryResultLocal>(www.downloadHandler.text);

            if (data == null || data.history == null || data.history.Count == 0)
            {
                ShowEmpty("Тарих жоқ / История пуста");
                yield break;
            }

            Debug.Log("ProfileHistory: записей = " + data.history.Count);

            // Очищаем старые строки
            foreach (Transform child in historyContent)
                Destroy(child.gameObject);

            foreach (HistoryEntryLocal entry in data.history)
                SpawnRow(entry);

            // Принудительно пересчитываем layout, чтобы Content растянулся
            if (historyContent is RectTransform rt)
                LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    private void SpawnRow(HistoryEntryLocal entry)
    {
        if (historyEntryPrefab == null || historyContent == null) return;

        GameObject row = Instantiate(historyEntryPrefab, historyContent);
        row.SetActive(true); // гарантируем что объект активен

        // Активируем все дочерние объекты (на случай если в префабе что-то отключено)
        foreach (Transform t in row.GetComponentsInChildren<Transform>(true))
            t.gameObject.SetActive(true);

        // DEBUG: выводим иерархию префаба чтобы видеть имена компонентов
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.AppendLine("[ProfileHistory] Prefab hierarchy:");
        foreach (Transform t in row.GetComponentsInChildren<Transform>(true))
            sb.AppendLine("  " + t.name + " (active=" + t.gameObject.activeSelf + ")");
        Debug.Log(sb.ToString());

        string text =
            $"{entry.scenario_name}\n" +
            $"<size=70%><color=#555555>{FormatDate(entry.completed_at)}</color></size>";

        // 1. Пробуем именованные дочерние TMP
        TextMeshProUGUI nameTxt = row.transform.Find("HistoryNameText")?.GetComponent<TextMeshProUGUI>();
        if (nameTxt != null)
        {
            nameTxt.text  = entry.scenario_name;
            nameTxt.color = Color.black;

            TextMeshProUGUI dateTxt  = row.transform.Find("HistoryDateText")?.GetComponent<TextMeshProUGUI>();
            TextMeshProUGUI stepsTxt = row.transform.Find("HistoryStepsText")?.GetComponent<TextMeshProUGUI>();
            if (dateTxt  != null) { dateTxt.text  = FormatDate(entry.completed_at); dateTxt.color = Color.black; }
            if (stepsTxt != null) { stepsTxt.text = entry.steps_total > 0 ? $"{entry.steps_done}/{entry.steps_total} қадам" : ""; stepsTxt.color = Color.black; }
            return;
        }

        // 2. Любой TMP внутри (Button → Text (TMP), простой Panel → Text и т.д.)
        TextMeshProUGUI[] allTmp = row.GetComponentsInChildren<TextMeshProUGUI>(true);
        if (allTmp.Length > 0)
        {
            allTmp[0].text  = text;
            allTmp[0].color = Color.black;
            allTmp[0].gameObject.SetActive(true);
            Debug.Log("[ProfileHistory] Set text on: " + allTmp[0].name + " → " + entry.scenario_name);
        }
        else
        {
            Debug.LogWarning("[ProfileHistory] TMP не найден в префабе " + historyEntryPrefab.name +
                             ". Добавьте TextMeshProUGUI в префаб!");
        }
    }

    private void ShowEmpty(string msg)
    {
        foreach (Transform child in historyContent)
            Destroy(child.gameObject);

        if (emptyLabel != null)
        {
            emptyLabel.text = msg;
            emptyLabel.gameObject.SetActive(true);
        }
    }

    private static string FormatDate(string raw)
    {
        if (System.DateTime.TryParse(raw, out System.DateTime dt))
            return dt.ToString("dd.MM.yyyy  HH:mm");
        return raw;
    }

    // ── Локальные модели (не зависят от ScenarioListController) ──────────────
    [System.Serializable]
    private class HistoryEntryLocal
    {
        public int    id;
        public int    scenario_id;
        public string scenario_name;
        public string completed_at;
        public int    steps_total;
        public int    steps_done;
    }

    [System.Serializable]
    private class HistoryResultLocal
    {
        public bool                   success;
        public List<HistoryEntryLocal> history;
    }
}
