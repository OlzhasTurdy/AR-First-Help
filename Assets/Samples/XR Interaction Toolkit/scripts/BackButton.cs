using UnityEngine;
using UnityEngine.SceneManagement;

public class BackButton : MonoBehaviour
{
    public void GoBack()
    {
        SceneManager.LoadScene("Untitled"); // им€ сцены
    }
    public void GoBackToOne()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex - 1);
    }
    public void GoBackScenario()
    {
        Debug.Log(" нопка нажата, пытаюсь загрузить сцену...");
        SceneManager.LoadScene("ScenarioSelection");
    }
    public void GoBuckCustomScenarios()
    {
        SceneManager.LoadScene("customScenarios");
    }
}