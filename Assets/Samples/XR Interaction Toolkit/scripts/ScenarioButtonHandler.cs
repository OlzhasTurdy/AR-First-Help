using UnityEngine;

public class ScenarioButtonHandler : MonoBehaviour
{
    // Этот метод мы повесим на кнопку
    public void SetScenarioAndLoad(string scenarioName)
    {
        // 1. Ищем НАСТОЯЩИЙ (выживший) синглтон и передаем ему данные
        if (ScenarioManager.Instance != null)
        {
            ScenarioManager.Instance.SelectScenario(scenarioName);
        }
        else
        {
            Debug.LogError("ScenarioManager не найден!");
        }

        // 2. Загружаем сцену (можете вызывать через свой SceneLoader, 
        // но можно и напрямую для экономии времени)
        UnityEngine.SceneManagement.SceneManager.LoadSceneAsync("ARScene");
    }
}