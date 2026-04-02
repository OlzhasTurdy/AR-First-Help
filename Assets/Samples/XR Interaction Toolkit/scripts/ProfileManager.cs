using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using TMPro;
using UnityEngine.UI;

// Класс для получения данных профиля от сервера
[System.Serializable]
public class UserProfileData
{
    public bool success;
    public string username;
    public string role;
    public int likes;
    public int finished;
    public string profile_pic_url;
    public string message;
}

public class ProfileManager : MonoBehaviour
{
    [Header("UI Elements")]
    public TMP_Text usernameText;
    public TMP_Text roleText;       // Первый "New Text"
    public TMP_Text likesText;      // Второй "New Text"
    public TMP_Text finishedText;   // Третий "New Text"
    public RawImage profileRawImage;

    private string serverURL = "https://autoreduce.kz";

    void Start()
    {
        // Запускаем загрузку данных при открытии экрана
        StartCoroutine(LoadProfileData());
    }

    IEnumerator LoadProfileData()
    {
        // Берем ID пользователя (если не залогинен - вернет 0, чтобы не грузить чужое)
        int currentUserId = PlayerPrefs.GetInt("userId", 0);

        if (currentUserId == 0)
        {
            usernameText.text = "Not Logged In";
            yield break; // Останавливаем загрузку
        }

        WWWForm form = new WWWForm();
        form.AddField("user_id", currentUserId.ToString());

        using (UnityWebRequest www = UnityWebRequest.Post(serverURL + "/get_profile.php", form))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                string json = www.downloadHandler.text;
                UserProfileData data = JsonUtility.FromJson<UserProfileData>(json);

                if (data.success)
                {
                    // Обновляем тексты
                    usernameText.text = data.username;
                    roleText.text = data.role;
                    likesText.text = "Likes:" + data.likes;
                    finishedText.text = "Finish scenarios: " + data.finished;

                    // Загружаем картинку, если ссылка не пустая
                    if (!string.IsNullOrEmpty(data.profile_pic_url))
                    {
                        StartCoroutine(DownloadAvatar(data.profile_pic_url));
                    }
                }
                else
                {
                    Debug.Log("Ошибка получения профиля: " + data.message);
                }
            }
            else
            {
                Debug.LogError("Ошибка связи с сервером: " + www.error);
            }
        }
    }

    IEnumerator DownloadAvatar(string url)
    {
        using (UnityWebRequest www = UnityWebRequestTexture.GetTexture(url))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                // Для Raw Image просто берем текстуру и назначаем её
                Texture2D downloadedTexture = DownloadHandlerTexture.GetContent(www);

                if (profileRawImage != null)
                {
                    profileRawImage.texture = downloadedTexture;
                }
            }
            else
            {
                Debug.LogError("Ошибка загрузки фото: " + www.error);
            }
        }
    }

    public void OnLogoutButton()
    {
        // Очищаем данные
        PlayerPrefs.DeleteKey("userId");
        PlayerPrefs.DeleteKey("lastLoginDate");
        PlayerPrefs.Save();

        // Переходим на сцену входа (убедись, что имя совпадает с твоим)
        UnityEngine.SceneManagement.SceneManager.LoadScene("loginregister");
    }
}
