using System.Collections.Generic;

// Структура одного шага
[System.Serializable]
public class CustomStep
{
    public string title;
    public string description;
    public string warnings;
    public string modelUrl;
}

// Структура всего сценария
[System.Serializable]
public class CustomScenario
{
    public string scenarioName;
    public List<CustomStep> steps = new List<CustomStep>();
}

// Статический класс для хранения данных МЕЖДУ сценами
public static class ScenarioDraft
{
    public static CustomScenario CurrentDraft = new CustomScenario();
}