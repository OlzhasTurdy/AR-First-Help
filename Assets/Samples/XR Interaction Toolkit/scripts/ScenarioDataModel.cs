using System.Collections.Generic;

[System.Serializable]
public class CustomStep
{
    public string title;
    public string description;
    public string warnings;
    public string modelUrl;
    public string videoUrl;   // �����
    public string quizRaw;   // �����: ����� ����� �����
}

[System.Serializable]
public class CustomScenario
{
    public string scenarioName;
    public List<CustomStep> steps = new List<CustomStep>();
}

public static class ScenarioDraft
{
    public static CustomScenario CurrentDraft = new CustomScenario();
}