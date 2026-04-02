using UnityEngine;
using UnityEngine.Networking;
using TMPro;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class ProfileScenarioListController : MonoBehaviour
{
    [Header("UI Elements")]
    public Transform contentPanel;   // Content из ScrollView в профиле
    public GameObject buttonPrefab;  // Префаб кнопки
    public GameObject loadingText;   // Текст "Загрузка..."
    public TextMeshProUGUI statusText; // (Опционально) Текст "У вас пока нет сценариев"

    void Start()
    {
        StartCoroutine(FetchUserScenarios());
    }

    private IEnumerator FetchUserScenarios()
    {
        if (loadingText != null) loadingText.SetActive(true);
        if (statusText != null) statusText.text = "";

        // Укажите URL к НОВОМУ PHP скрипту, который фильтрует по user_id
        string url = "https://autoreduce.kz/get_user_scenarios.php";

        // Берем ID текущего пользователя (1 по умолчанию, если не залогинен)
        int currentUserId = PlayerPrefs.GetInt("userId", 1);

        // В отличие от обычного GET, мы используем форму (POST), 
        // чтобы передать user_id на сервер
        WWWForm form = new WWWForm();
        form.AddField("user_id", currentUserId.ToString());

        using (UnityWebRequest www = UnityWebRequest.Post(url, form))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                string jsonResponse = www.downloadHandler.text;
                Debug.Log("Сценарии пользователя получены: " + jsonResponse);

                
                ScenarioDBResult result = JsonUtility.FromJson<ScenarioDBResult>(jsonResponse);

                if (result.items == null || result.items.Count == 0)
                {
                    if (statusText != null) statusText.text = "У вас пока нет сохраненных сценариев.";
                }
                else
                {
                    
                    PopulateList(result.items);
                }
            }
            else
            {
                Debug.LogError("Ошибка загрузки профиля: " + www.error);
                if (statusText != null) statusText.text = "Ошибка подключения к серверу.";
            }
        }

        if (loadingText != null) loadingText.SetActive(false);
    }

    private void PopulateList(List<ScenarioDBItem> scenarios)
    {
        // Очищаем старые элементы
        foreach (Transform child in contentPanel)
        {
            Destroy(child.gameObject);
        }

        // Создаем кнопку для каждого сценария
        foreach (ScenarioDBItem sc in scenarios)
        {
            GameObject newBtnObj = Instantiate(buttonPrefab, contentPanel);

            TextMeshProUGUI btnText = newBtnObj.GetComponentInChildren<TextMeshProUGUI>();
            if (btnText != null)
            {
                btnText.text = sc.scenario_name;
            }

            Button btn = newBtnObj.GetComponent<Button>();
            string savedJsonData = sc.json_data;

            btn.onClick.AddListener(() => OnScenarioButtonClicked(savedJsonData));
        }
    }

    private void OnScenarioButtonClicked(string jsonData)
    {
        Debug.Log("Выбран свой сценарий. Загружаем данные...");

        CustomScenario loadedScenario = JsonUtility.FromJson<CustomScenario>(jsonData);
        ScenarioManager.GetInstance().SelectCustomScenario(loadedScenario);

        // Переходим в AR сцену
        SceneManager.LoadScene("ARScene");
    }
}
