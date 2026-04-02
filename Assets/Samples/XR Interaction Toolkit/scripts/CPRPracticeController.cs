using UnityEngine;
using TMPro;

public class CPRPracticeController : MonoBehaviour
{
    private TextMeshProUGUI feedbackText;
    private TextMeshProUGUI counterText;
    private TextMeshProUGUI resultText;

    [Header("Settings")]
    public int targetCompressions = 30;
    public float minInterval = 0.5f;
    public float maxInterval = 0.7f;

    private int compressionCount = 0;
    private float lastPressTime = 0f;
    private float totalIntervalTime = 0f;

    private bool practiceActive = false;

    // 🔥 ВОТ ЭТОГО МЕТОДА У ТЕБЯ СЕЙЧАС НЕТ
    public void Initialize(
        TextMeshProUGUI feedback,
        TextMeshProUGUI counter,
        TextMeshProUGUI result)
    {
        feedbackText = feedback;
        counterText = counter;
        resultText = result;
    }

    public void StartPractice()
    {
        compressionCount = 0;
        lastPressTime = 0f;
        totalIntervalTime = 0f;
        practiceActive = true;

        if (counterText) counterText.text = "Count: 0";
        if (feedbackText) feedbackText.text = "Start Compressions!";
        if (resultText) resultText.text = "";
    }

    void Update()
    {
        if (!practiceActive) return;

        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);

            if (touch.phase == TouchPhase.Began)
            {
                Ray ray = Camera.main.ScreenPointToRay(touch.position);
                RaycastHit hit;

                // Внутри Update в CPRPracticeController.cs измени это условие:
                if (Physics.Raycast(ray, out hit))
                {
                    // Вместо if (hit.transform == transform) пишем:
                    if (hit.collider.CompareTag("CPRTarget"))
                    {
                        RegisterCompression();
                    }
                }
            }
        }
    }

    void RegisterCompression()
    {
        float currentTime = Time.time;

        if (lastPressTime > 0f)
        {
            float interval = currentTime - lastPressTime;
            totalIntervalTime += interval;

            if (interval < minInterval)
                feedbackText.text = "Slower!";
            else if (interval > maxInterval)
                feedbackText.text = "Faster!";
            else
                feedbackText.text = "Good Rhythm";
        }

        lastPressTime = currentTime;

        compressionCount++;
        counterText.text = "Count: " + compressionCount;

        if (compressionCount >= targetCompressions)
            FinishPractice();
    }

    void FinishPractice()
    {
        practiceActive = false;

        float averageInterval = totalIntervalTime / (compressionCount - 1);
        float bpm = 60f / averageInterval;

        string grade;

        if (bpm >= 100f && bpm <= 120f)
            grade = "Excellent";
        else if (bpm >= 90f && bpm <= 130f)
            grade = "Good";
        else
            grade = "Needs Improvement";

        resultText.text =
            "Practice Completed!\n" +
            "Average BPM: " + bpm.ToString("F0") +
            "\nResult: " + grade;
    }
}