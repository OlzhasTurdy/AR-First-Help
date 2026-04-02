using UnityEngine;
using UnityEngine.Networking;
using TMPro;
using System.Collections;
using UnityEngine.SceneManagement;
using System;

public class AuthManager : MonoBehaviour
{
    [Header("Login Inputs")]
    public TMP_InputField loginEmailInput;
    public TMP_InputField loginPasswordInput;

    [Header("Register Inputs")]
    public TMP_InputField registerUsernameInput;
    public TMP_InputField registerEmailInput;
    public TMP_InputField registerPasswordInput;

    public TMP_Text statusText;

    private string serverURL = "https://autoreduce.kz";

    // ================= REGISTER =================

    public void OnRegisterButton()
    {
        StartCoroutine(Register());
    }
    void Start()
    {
        if (PlayerPrefs.HasKey("userId") && PlayerPrefs.HasKey("lastLoginDate"))
        {
            long binary = Convert.ToInt64(PlayerPrefs.GetString("lastLoginDate"));
            DateTime lastLogin = DateTime.FromBinary(binary);

            TimeSpan difference = DateTime.UtcNow - lastLogin;

            if (difference.TotalDays < 10)
            {
                SceneManager.LoadScene("Untitled");
            }
            else
            {
                PlayerPrefs.DeleteKey("userId");
                PlayerPrefs.DeleteKey("lastLoginDate");
            }
        }
    }

    IEnumerator Register()
    {
        RegisterData data = new RegisterData(
            registerUsernameInput.text,
            registerEmailInput.text,
            registerPasswordInput.text
        );

        string json = JsonUtility.ToJson(data);

        UnityWebRequest request = new UnityWebRequest(serverURL + "/registerunity.php", "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);

        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            statusText.text = "Connection error";
            yield break;
        }

        Response response = JsonUtility.FromJson<Response>(
            request.downloadHandler.text
        );

        if (response.success)
        {
            statusText.text = "Registered successfully!";
            PlayerPrefs.SetInt("userId", response.userId);
            PlayerPrefs.Save();
        }
        else
        {
            statusText.text = response.message;
        }
    }


    // ================= LOGIN =================

    public void OnLoginButton()
    {
        StartCoroutine(Login());
    }

    IEnumerator Login()
    {
        LoginData data = new LoginData(
            loginEmailInput.text,
            loginPasswordInput.text
        );

        string json = JsonUtility.ToJson(data);

        UnityWebRequest request = new UnityWebRequest(serverURL + "/loginunity.php", "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);

        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            statusText.text = "Connection error";
            yield break;
        }

        Response response = JsonUtility.FromJson<Response>(
            request.downloadHandler.text
        );

        if (response.success)
        {
            statusText.text = "Welcome " + response.username;
            PlayerPrefs.SetInt("userId", response.userId);
            PlayerPrefs.SetString("lastLoginDate", DateTime.UtcNow.ToBinary().ToString());
            PlayerPrefs.SetString("user_role", response.role);
            PlayerPrefs.Save();

            yield return new WaitForSeconds(1f);
            SceneManager.LoadScene("Untitled");
        }
        else
        {
            statusText.text = response.message;
        }
        
    }

}


// ================= DATA CLASSES =================

[System.Serializable]
public class RegisterData
{
    public string username;
    public string email;
    public string password;

    public RegisterData(string u, string e, string p)
    {
        username = u;
        email = e;
        password = p;
    }
}

[System.Serializable]
public class LoginData
{
    public string email;
    public string password;

    public LoginData(string e, string p)
    {
        email = e;
        password = p;
    }
}

[System.Serializable]
public class Response
{
    public bool success;
    public string message;
    public int userId;
    public string username;
    public string role;
    public int likes;
    public int finished;
    public string profile_pic_url;
}

