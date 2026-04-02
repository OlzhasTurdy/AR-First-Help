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
        isCustomScenario = false;
        selectedScenario = scenarioName;
    }

    // НОВЫЙ Метод для запуска сценария из базы данных
    public void SelectCustomScenario(CustomScenario customData)
    {
        isCustomScenario = true;
        currentCustomScenario = customData;
    }
}
