using UnityEngine;
using UnityEngine.UI;

public class ModelSlotController : MonoBehaviour
{
    public Button slotButton;

    public GameObject cprModel;
    public GameObject chokingModel;
    public GameObject bleedingModel;
    public GameObject unconsciousModel;

    public Transform modelSpawnPoint;

    private GameObject currentModel;
    private string activeScenario;

    void Start()
    {
        if (slotButton != null)
            slotButton.onClick.AddListener(ToggleModel);
            Debug.Log("ToggleModel called");

    }

    public void SetScenario(string scenario)
    {
        activeScenario = scenario;

        if (currentModel != null)
        {
            Destroy(currentModel);
            currentModel = null;
        }
    }

    void ToggleModel()
    {
        if (currentModel != null)
        {
            Destroy(currentModel);
            currentModel = null;
            return;
        }

        GameObject prefabToSpawn = null;

        switch (activeScenario)
        {
            case "CPR":
                prefabToSpawn = cprModel;
                Debug.Log("CPR Scenario");
                break;
            case "Choking":
                prefabToSpawn = chokingModel;
                Debug.Log("ToggleModel called");
                break;
            case "Bleeding":
                prefabToSpawn = bleedingModel;
                Debug.Log("ToggleModel called");
                break;
            case "Unconscious":
                prefabToSpawn = unconsciousModel;
                Debug.Log("ToggleModel called");
                break;
        }

        if (prefabToSpawn != null && modelSpawnPoint != null)
        {
            Debug.Log("Spawning model: " + prefabToSpawn);
            currentModel = Instantiate(prefabToSpawn, modelSpawnPoint.position, modelSpawnPoint.rotation);

            currentModel.AddComponent<PreviewModelRotator>();


            int layer = LayerMask.NameToLayer("ModelPreviewLayer");
            SetLayerRecursively(currentModel, layer);
        }
    }

    void SetLayerRecursively(GameObject obj, int layer)
    {
        obj.layer = layer;

        foreach (Transform child in obj.transform)
        {
            SetLayerRecursively(child.gameObject, layer);

        }
    }
}
