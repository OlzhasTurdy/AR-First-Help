using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    public void LoadAR()
    {
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
}
