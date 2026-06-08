using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using TMPro;

[System.Serializable]
public class ProfileStatsData
{
    public bool success;
    public int likes;
    public int finished;
    public string message;
}

/// <summary>
/// Отдельный скрипт для загрузки только статистики (Лайки и Пройденные сценарии).
/// Повесьте его на любой объект в сцене профиля.
/// </summary>
public class ProfileStatsController : MonoBehaviour
{
    [Header("UI Elements")]
    public TextMeshProUGUI likesText;
    public TextMeshProUGUI finishedText;
    
    private string serverURL = "https://autoreduce.kz/get_profile_stats.php";

    void Start()
    {
        StartCoroutine(LoadStats());
    }

    IEnumerator LoadStats()
    {
        // Получаем ID пользователя
        int userId = PlayerPrefs.GetInt("CurrentUserID", PlayerPrefs.GetInt("userId", 1));

        WWWForm form = new WWWForm();
        form.AddField("user_id", userId.ToString());

        using (UnityWebRequest www = UnityWebRequest.Post(serverURL, form))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                ProfileStatsData data = JsonUtility.FromJson<ProfileStatsData>(www.downloadHandler.text);
                if (data != null && data.success)
                {
                    if (likesText != null) likesText.text = "Likes: " + data.likes;
                    if (finishedText != null) finishedText.text = "Finish scenarios: " + data.finished;
                }
                else
                {
                    Debug.Log("Ошибка получения статистики: " + (data != null ? data.message : "Пустой ответ"));
                }
            }
            else
            {
                Debug.LogError("Ошибка связи с сервером при загрузке статистики: " + www.error);
            }
        }
    }
}
