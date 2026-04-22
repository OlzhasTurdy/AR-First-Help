using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    public void LoadAR()
    {
        ResetScenarioIfNeeded();
        SceneManager.LoadScene("ScenarioSelection");
    }
    public void LoadARSC()
    {
        SceneManager.LoadSceneAsync("ARScene");
    }
    public void LoadQuiz()
    {
        SceneManager.LoadScene("Quiz");
    }

    public void LoadTheory()
    {
        SceneManager.LoadScene("selectiontheory");
    }
    public void LoadProfile()
    {
        SceneManager.LoadScene("ProfileScene");
    }

    public void LoadMenu()
    {
        // Обязательно сбрасываем данные при возврате в главное меню
        if (ScenarioManager.Instance != null)
        {
            ScenarioManager.Instance.ReturnToMainMenu();
        }
        SceneManager.LoadScene("MainMenu");
    }
    public void LoadScenario()
    {
        SceneManager.LoadScene("customScenarios");
    }
    public void LoadCreateScenario()
    {
        SceneManager.LoadScene("selectName");
    }
    public void LoadMediaPipe()
    {
        SceneManager.LoadScene("Pose Tracking");
    }
    public void Hans()
    {
        SceneManager.LoadScene("Theory");
    }
    private void ResetScenarioIfNeeded()
    {
        if (ScenarioManager.Instance != null)
        {
            ScenarioManager.Instance.ResetScenarioData();
        }
    }
}
