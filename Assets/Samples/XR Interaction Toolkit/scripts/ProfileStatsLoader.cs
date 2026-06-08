using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using TMPro;

/// <summary>
/// Скрипт для подсчета лайков и завершенных сценариев без необходимости менять бекенд.
/// Он использует уже существующие эндпоинты (get_scenarios.php и scenario_history.php).
/// </summary>
public class ProfileStatsLoader : MonoBehaviour
{
    [Header("UI Elements")]
    public TextMeshProUGUI likesText;
    public TextMeshProUGUI finishedText;

    private const string BASE_URL = "https://autoreduce.kz/";

    void Start()
    {
        StartCoroutine(LoadStats());
    }

    private IEnumerator LoadStats()
    {
        int userId = PlayerPrefs.GetInt("CurrentUserID", PlayerPrefs.GetInt("userId", 1));

        if (likesText != null) likesText.text = "Likes: ...";
        if (finishedText != null) finishedText.text = "Finish scenarios: ...";

        // 1. Считаем пройденные сценарии (через scenario_history.php)
        string historyUrl = $"{BASE_URL}scenario_history.php?action=get&user_id={userId}";
        using (UnityWebRequest wwwHistory = UnityWebRequest.Get(historyUrl))
        {
            yield return wwwHistory.SendWebRequest();

            if (wwwHistory.result == UnityWebRequest.Result.Success)
            {
                HistoryResult hResult = JsonUtility.FromJson<HistoryResult>(wwwHistory.downloadHandler.text);
                if (hResult != null && hResult.success && hResult.history != null)
                {
                    if (finishedText != null)
                        finishedText.text = "Finish scenarios: " + hResult.history.Count;
                }
                else
                {
                    if (finishedText != null) finishedText.text = "Finish scenarios: 0";
                }
            }
        }

        // 2. Считаем поставленные лайки (через get_scenarios.php)
        string scenariosUrl = $"{BASE_URL}get_scenarios.php?user_id={userId}";
        using (UnityWebRequest wwwScenarios = UnityWebRequest.Get(scenariosUrl))
        {
            yield return wwwScenarios.SendWebRequest();

            if (wwwScenarios.result == UnityWebRequest.Result.Success)
            {
                ScenarioDBResult sResult = JsonUtility.FromJson<ScenarioDBResult>(wwwScenarios.downloadHandler.text);
                if (sResult != null && sResult.items != null)
                {
                    int myLikesCount = 0;
                    foreach (var item in sResult.items)
                    {
                        if (item.isLiked)
                        {
                            myLikesCount++;
                        }
                    }

                    if (likesText != null)
                        likesText.text = "Likes: " + myLikesCount;
                }
                else
                {
                    if (likesText != null) likesText.text = "Likes: 0";
                }
            }
        }
    }

    // Вспомогательные классы для десериализации (такие же как в ScenarioListController)
    [System.Serializable]
    private class HistoryEntry
    {
        public int id;
    }

    [System.Serializable]
    private class HistoryResult
    {
        public bool success;
        public List<HistoryEntry> history;
    }

    [System.Serializable]
    private class ScenarioDBItem
    {
        public bool isLiked;
    }

    [System.Serializable]
    private class ScenarioDBResult
    {
        public List<ScenarioDBItem> items;
    }
}
