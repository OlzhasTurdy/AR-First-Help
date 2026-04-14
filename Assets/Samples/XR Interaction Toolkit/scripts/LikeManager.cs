using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.Collections;

public class LikeManager : MonoBehaviour
{
    [Header("UI Elements")]
    public Image buttonImage;      // Сюда перетащите Image этой кнопки
    public Sprite likedSprite;     // Иконка "Лайкнуто" (закрашенная)
    public Sprite unlikedSprite;   // Иконка "Пустая"

    [Header("Data (Fill from Controller)")]
    public int userId;
    public int scenarioId;
    public string phpUrl = "https://autoreduce.kz/like_logic.php";

    private bool isLiked = false;

    // Этот метод вызывается из ScenarioListController после спавна кнопки
    public void InitStatus(int uId, int sId)
    {
        userId = uId;
        scenarioId = sId;

        // Сначала ставим пустую иконку, пока ждем ответ сервера
        isLiked = false;
        UpdateButtonVisuals();

        // Проверяем, лайкал ли этот пользователь сценарий ранее
        StartCoroutine(CheckInitialLikeStatus());
    }

    public void OnLikeButtonClick()
    {
        // Мгновенная реакция UI (Optimistic UI)
        isLiked = !isLiked;
        UpdateButtonVisuals();

        // Отправка запроса на сервер
        StartCoroutine(SendLikeStatusToServer(isLiked));
    }

    private void UpdateButtonVisuals()
    {
        if (buttonImage != null)
        {
            buttonImage.sprite = isLiked ? likedSprite : unlikedSprite;
        }
    }

    IEnumerator CheckInitialLikeStatus()
    {
        string checkUrl = $"{phpUrl}?user_id={userId}&scenario_id={scenarioId}&action=check";

        using (UnityWebRequest www = UnityWebRequest.Get(checkUrl))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                string response = www.downloadHandler.text.Trim();
                isLiked = (response == "1");
                UpdateButtonVisuals();
            }
        }
    }

    IEnumerator SendLikeStatusToServer(bool status)
    {
        WWWForm form = new WWWForm();
        form.AddField("user_id", userId);
        form.AddField("scenario_id", scenarioId);
        form.AddField("action", status ? "like" : "unlike");

        using (UnityWebRequest www = UnityWebRequest.Post(phpUrl, form))
        {
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Ошибка лайка: " + www.error);
                // Если сервер не ответил, откатываем иконку назад
                isLiked = !status;
                UpdateButtonVisuals();
            }
        }
    }
}