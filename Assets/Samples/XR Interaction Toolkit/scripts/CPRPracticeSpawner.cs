using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using System.Collections.Generic;
using TMPro;

public class CPRPracticeSpawner : MonoBehaviour
{
    public GameObject practicePrefab;
    public ARRaycastManager raycastManager;

    public TextMeshProUGUI feedbackText;
    public TextMeshProUGUI counterText;
    public TextMeshProUGUI resultText;

    private static List<ARRaycastHit> hits = new List<ARRaycastHit>();
    private GameObject spawnedObject;

    private bool waitingForPlacement = false;

    public void StartPracticePlacement()
    {
        Debug.Log("Practice Button Pressed");
        waitingForPlacement = true;
    }

    void Update()
    {
        if (!waitingForPlacement) return;

        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);

            if (touch.phase == TouchPhase.Began)
            {
                if (raycastManager.Raycast(touch.position, hits, TrackableType.PlaneWithinPolygon))
                {
                    Pose hitPose = hits[0].pose;

                    spawnedObject = Instantiate(practicePrefab, hitPose.position, hitPose.rotation);
                    spawnedObject.transform.localScale = Vector3.one * 0.05f;

                    // 🔥 НАСТРОЙКА UI
                    CPRPracticeController controller =
                        spawnedObject.GetComponentInChildren<CPRPracticeController>();

                    controller.Initialize(feedbackText, counterText, resultText);
                    controller.StartPractice();

                    waitingForPlacement = false;
                }
            }
        }
    }
}