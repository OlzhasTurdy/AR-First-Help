using UnityEngine;
using UnityEngine.Networking;
using TMPro;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

// Вспомогательные классы для парсинга JSON от PHP
[System.Serializable]
public class ScenarioDBItem
{
    public int id;
    public string scenario_name;
    public string json_data;
    public int likes;
    public int views;
}

[System.Serializable]
public class ScenarioDBResult
{
    public List<ScenarioDBItem> items;
}

public class ScenarioListController : MonoBehaviour
{
    [Header("UI Elements")]
    public Transform contentPanel;   // Сюда перетащите объект Content из ScrollView
    public GameObject buttonPrefab;  // Сюда перетащите сохраненный префаб кнопки
    public GameObject loadingText;   // (Опционально) Текст "Загрузка..."

    void Start()
    {
        // Как только экран открылся, скачиваем список
        StartCoroutine(FetchScenarios());
    }

    private IEnumerator FetchScenarios()
    {
        if (loadingText != null) loadingText.SetActive(true);

        string url = "https://autoreduce.kz/get_scenarios.php";

        using (UnityWebRequest www = UnityWebRequest.Get(url))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                // Получаем ответ от PHP
                string jsonResponse = www.downloadHandler.text;
                Debug.Log("Получено от сервера: " + jsonResponse);

                // Превращаем JSON в объекты C#
                ScenarioDBResult result = JsonUtility.FromJson<ScenarioDBResult>(jsonResponse);

                // Отрисовываем кнопки
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

            // 1. Основной текст (Имя и просмотры)
            TextMeshProUGUI btnText = newBtnObj.GetComponentInChildren<TextMeshProUGUI>();
            if (btnText != null)
            {
                btnText.text = $"{sc.scenario_name} (Views {sc.views})";
            }

            // 2. Логика основной кнопки (Запуск AR)
            Button mainBtn = newBtnObj.GetComponent<Button>();
            int idForButton = sc.id;
            string jsonForButton = sc.json_data;
            string nameForButton = sc.scenario_name;

            mainBtn.onClick.AddListener(() => OnScenarioButtonClicked(idForButton, jsonForButton));

            // 3. Логика кнопки комментариев (Ищем дочернюю кнопку "InfoButton")
            // Предположим, вы добавили её в префаб и назвали "InfoButton"
            Button infoBtn = newBtnObj.transform.Find("InfoButton")?.GetComponent<Button>();

            if (infoBtn != null)
            {
                infoBtn.onClick.AddListener(() => OnCommentsButtonClicked(idForButton, nameForButton));
            }
        }
    }

    // Метод для перехода к комментариям
    private void OnCommentsButtonClicked(int scenarioId, string scenarioName)
    {
        Debug.Log("Переход к комментариям сценария: " + scenarioId);

        // Сохраняем ID и Имя, чтобы сцена комментариев знала, что загружать
        PlayerPrefs.SetInt("SelectedScenarioID", scenarioId);
        PlayerPrefs.SetString("SelectedScenarioName", scenarioName);

        // Загружаем сцену с комментариями (проверьте название сцены!)
        SceneManager.LoadScene("CommentScene");
    }

    private void OnScenarioButtonClicked(int scenarioId,string jsonData)
    {
        StartCoroutine(UpdateViewCount(scenarioId));

        // 1. Распаковываем сам сценарий из JSON-строки
        CustomScenario loadedScenario = JsonUtility.FromJson<CustomScenario>(jsonData);

        // 2. Передаем его в наш глобальный менеджер (который мы обновляли в прошлом шаге)
        ScenarioManager.GetInstance().SelectCustomScenario(loadedScenario);

        // 3. Загружаем вашу сцену с AR (напишите сюда правильное название вашей AR-сцены!)
        SceneManager.LoadScene("ARScene");
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
}