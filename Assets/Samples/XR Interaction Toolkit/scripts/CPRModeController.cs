using UnityEngine;

public class CPRModeController : MonoBehaviour
{
    public GameObject demoGroup;
    public GameObject practiceGroup;

    void Start()
    {
        ShowDemo();
    }

    public void ShowDemo()
    {
        demoGroup.SetActive(true);
        practiceGroup.SetActive(false);
    }

    public void ShowPractice()
    {
        demoGroup.SetActive(false);
        practiceGroup.SetActive(true);
    }
}
