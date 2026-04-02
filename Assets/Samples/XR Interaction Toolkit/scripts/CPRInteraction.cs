using UnityEngine;
using TMPro;
using System.Collections;

public class CPRInteraction : MonoBehaviour
{
    public TextMeshProUGUI feedbackText;

    [Header("Chest Animation")]
    public Transform chestTransform;

    private Vector3 originalScale;
    private Vector3 compressedScale;

    private int compressionCount = 0;
    private float lastPressTime = -1f;
    private int correctTempoCount = 0;

    [Header("Tempo Settings")]
    public float minInterval = 0.45f;
    public float maxInterval = 0.75f;

    void Start()
    {
        if (chestTransform != null)
        {
            originalScale = chestTransform.localScale;
            compressedScale = originalScale * 0.9f; // 10% сжатие
        }
    }

    void Update()
    {
        if (Input.touchCount == 0) return;

        Touch touch = Input.GetTouch(0);

        if (touch.phase != TouchPhase.Began) return;

        Ray ray = Camera.main.ScreenPointToRay(touch.position);

        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            if (hit.collider.CompareTag("Chest"))
            {
                RegisterCompression();
                StartCoroutine(CompressChest());
            }
        }
    }

    void RegisterCompression()
    {
        float currentTime = Time.time;

        if (lastPressTime > 0)
        {
            float interval = currentTime - lastPressTime;

            if (interval >= minInterval && interval <= maxInterval)
            {
                correctTempoCount++;
            }
        }

        lastPressTime = currentTime;
        compressionCount++;

        feedbackText.text = $"Compressions: {compressionCount}";

        if (compressionCount >= 30)
        {
            FinishCPR();
        }
    }

    IEnumerator CompressChest()
    {
        if (chestTransform == null) yield break;

        chestTransform.localScale = compressedScale;
        yield return new WaitForSeconds(0.1f);
        chestTransform.localScale = originalScale;
    }

    void FinishCPR()
    {
        float tempoScore = ((float)correctTempoCount / compressionCount) * 100f;

        feedbackText.text =
            $"CPR Completed!\n" +
            $"Tempo Accuracy: {tempoScore:F0}%";
    }
}
