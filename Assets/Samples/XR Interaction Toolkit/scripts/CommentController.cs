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
    public TextMeshProUGUI authorText; // ���������� ���� ������ "Author"
    public TextMeshProUGUI likesText;  // ���������� ���� ������ "Likes"
    public TextMeshProUGUI viewsText;  // ���������� ���� ������ "views"

    [Header("Comments UI")]
    public Transform contentPanel;
    public GameObject commentPrefab;
    public TMP_InputField newCommentInput;

    private int scenarioId;

    void Start()
    {
        // ����� ID �� PlayerPrefs, ������� �� ��������� ��� ����� � ������
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

                // ��������� "�����" ������� �� ��
                titleText.text = data.info.scenario_name;
                if (authorText) authorText.text = "By: " + data.info.username;
                if (likesText) likesText.text = "Likes: " + data.info.likes;
                if (viewsText) viewsText.text = "Views: " + data.info.views;

                // ������� � ��������� ������ ������������
                foreach (Transform child in contentPanel) Destroy(child.gameObject);
                foreach (var c in data.comments)
                {
                    GameObject go = Instantiate(commentPrefab, contentPanel);
                    TextMeshProUGUI[] t = go.GetComponentsInChildren<TextMeshProUGUI>();
                    if (t.Length >= 2)
                    {
                        t[0].text = c.username;     // ��� ������������
                        t[1].text = c.comment_text; // ����� �����������
                    }
                }
            }
        }
    }

    // ����� ��� Enter � InputField
    // ����� ��� ������ (��������� ��� � OnClick �������� ������)
    public void OnSendButtonClick()
    {
        string text = newCommentInput.text;
        if (!string.IsNullOrWhiteSpace(text))
        {
            StartCoroutine(PostComment(text));
        }
    }

    // ����� ��� Enter (�������� ��� � On End Edit � InputField)
    public void OnSubmitComment(string text)
    {
        // �� ��������� OnEndEdit ����������� �����, ������� ��������� �������
        if (!string.IsNullOrWhiteSpace(text) && (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)))
        {
            StartCoroutine(PostComment(text));
        }
    }

    IEnumerator PostComment(string text)
    {
        // ��������� ����, ����� ������������ �� ����� ������
        newCommentInput.interactable = false;

        WWWForm form = new WWWForm();
        form.AddField("scenario_id", scenarioId);

        // ����� ID �������� ������������
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
                StartCoroutine(LoadAllData()); // ��������� ������, ����� ������� ����� �������
                Debug.Log("������� ���������: " + www.downloadHandler.text);
            }
            else
            {
                Debug.LogError("������ �������� ��������: " + www.error);
            }
        }
    }
}