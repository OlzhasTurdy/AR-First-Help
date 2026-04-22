using UnityEngine;

public class ScenarioManager : MonoBehaviour
{
    public static ScenarioManager Instance;

    public string selectedScenario;

    
    public bool isCustomScenario = false;
    public CustomScenario currentCustomScenario;
    

    private void Awake()
    {
        Application.targetFrameRate = 60;
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
    }

    
    public static ScenarioManager GetInstance()
    {
        if (Instance == null)
        {
            GameObject go = new GameObject("ScenarioManager");
            Instance = go.AddComponent<ScenarioManager>();
        }
        return Instance;
    }


    public void SelectScenario(string scenarioName)
    {
        // Сбрасываем предыдущие данные
        ResetScenarioData();

        isCustomScenario = false;
        selectedScenario = scenarioName;

        Debug.Log($"Selected standard scenario: {scenarioName}");
    }

    public void SelectCustomScenario(CustomScenario customData)
    {
        // Сбрасываем предыдущие данные
        ResetScenarioData();

        isCustomScenario = true;
        currentCustomScenario = customData;

        Debug.Log($"Selected custom scenario: {customData?.scenarioName ?? "null"}");
    }

    // НОВЫЙ Метод для полного сброса состояния
    public void ResetScenarioData()
    {
        selectedScenario = null;
        isCustomScenario = false;
        currentCustomScenario = null;

        Debug.Log("ScenarioManager data reset");
    }

    // НОВЫЙ Метод для очистки при возврате в главное меню
    public void ReturnToMainMenu()
    {
        ResetScenarioData();
    }
}
