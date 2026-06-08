using GLTFast;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

[System.Serializable]
public class QuizData
{
    public string question;
    public string[] answers;
    public int correctAnswerIndex;
    public float timeLimit = 15f;
}

public enum PatientStateDecision
{
    None,
    PrimaryAssessment,
    EmergencyTypeAssessment,
    BreathingProblemAssessment,
    UnconsciousProblemAssessment
}

public enum PatientStateBranch
{
    Responsive,
    UnconsciousBreathing,
    NoBreathingNoPulse,
    Bleeding,
    Choking,
    CannotBreathe,
    Unconscious,
    BreathingProblem,
    UnconsciousProblem
}

[System.Serializable]
public class ScenarioStep
{
    public string title;

    [TextArea(3, 10)]
    public string description;

    // ЖАҢА — Толық ақпараты бар үлкен мәтін
    [TextArea(5, 15)]
    public string information;

    [TextArea(2, 6)]
    public string warnings;

    public Sprite stepImage;

    public GameObject stepPrefab;

    public string modelUrl;

    public bool enableBodyTracking;

    public QuizData quiz;

    public VideoClip stepVideoClip;

    public PatientStateDecision patientStateDecision = PatientStateDecision.None;
}

public class ScenarioController : MonoBehaviour
{
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI stepTitleText;
    public TextMeshProUGUI descriptionText;

    // ЖАҢА — Толық ақпарат үшін UI
    public TextMeshProUGUI informationText;

    public TextMeshProUGUI warningText;

    [Header("Video Panel")]
    public VideoPanelAnimator videoPanelAnimator;

    public Image stepImageUI;
    public Button nextButton;
    public Button prevButton;
    public GameObject practiceButton;
    public GameObject watchButton;

    [Header("Level Complete Effect")]
    public LevelCompleteEffect levelCompleteEffect;

    // --- Тест үшін UI ---
    [Header("Quiz UI")]
    public GameObject quizPanel;
    public TextMeshProUGUI quizQuestionText;
    public TextMeshProUGUI timerText;
    public Button[] answerButtons;

    // СЛР қадамдары үшін Prefab-тар
    public GameObject cprSceneAssessmentPrefab;
    public GameObject cprResponsiveCheckPrefab;
    public GameObject cprCheckBreathingPrefab;
    public GameObject cprCallEmergencyPrefab;
    public GameObject cprChestCompressionsPrefab;
    public GameObject cprRescueBreathsPrefab;
    public GameObject cprAEDPrefab;

    // Қалған сценарийлер
    public GameObject chokingPrefab;
    public GameObject bleedingPrefab;
    public GameObject unconsciousPrefab;

    [Header("AR")]
    public ARRaycastManager raycastManager;

    private static List<ARRaycastHit> hits = new List<ARRaycastHit>();

    private GameObject currentModel;
    private GameObject pendingPrefab;
    private string pendingModelUrl; // Жүктеу үшін экранды түртуді күтеді

    [Header("IMGS")]
    public Sprite safetySprite;
    public Sprite checkBreathingSprite;
    public Sprite heimlichSprite;

    // БАРЛЫҚ ҚАДАМДАР ҮШІН ЖАҢА СПРАЙТТАР
    public Sprite sceneAssessmentSprite;
    public Sprite responsiveCheckSprite;
    public Sprite callEmergencySprite;
    public Sprite chestCompressionsSprite;
    public Sprite rescueBreathsSprite;
    public Sprite aedSprite;

    // "Тұншығу" сценарийі үшін спрайттар
    public Sprite chokingAssessmentSprite;
    public Sprite backBlowsSprite;
    public Sprite abdominalThrustsSprite;
    public Sprite chokingCollapseSprite;

    // "Қан кету" сценарийі үшін спрайттар
    public Sprite directPressureSprite;
    public Sprite woundPackingSprite;
    public Sprite tourniquetSprite;
    public Sprite shockPreventionSprite;

    // "Ес-түссіз" сценарийі үшін спрайттар
    public Sprite unconsciousBreathingSprite;
    public Sprite secondarySurveySprite;
    public Sprite recoveryPositionSprite;
    public Sprite monitoringSprite;

    public float typingSpeed = 0.02f;
    [Header("Interaction Settings")]
    public float rotationSpeed = 0.5f;

    private List<ScenarioStep> steps = new List<ScenarioStep>();
    private int currentStepIndex = 0;

    // --- Таймер айнымалылары ---
    private bool isQuizActive = false;
    private bool isPatientStateChoiceActive = false;
    private float timeLeft;

    [Header("Choking Prefabs — one per step")]
    public GameObject chokingAssessPrefab;       // Step 1
    public GameObject chokingBackBlowsPrefab;    // Step 2
    public GameObject chokingHeimlichPrefab;     // Step 3
    public GameObject chokingFingerSweepPrefab;  // Step 4
    public GameObject chokingCollapsesPrefab;    // Step 5

    [Header("Bleeding Prefabs — one per step")]
    public GameObject bleedingDirectPressurePrefab;  // Step 1
    public GameObject bleedingWoundPackingPrefab;     // Step 2
    public GameObject bleedingTourniquetPrefab;       // Step 3
    public GameObject bleedingShockPrefab;            // Step 4

    [Header("Unconscious Prefabs — one per step")]
    public GameObject unconsciousResponsePrefab;      // Step 1
    public GameObject unconsciousAirwayPrefab;        // Step 2
    public GameObject unconsciousBreathCheckPrefab;   // Step 3
    public GameObject unconsciousRecoveryPrefab;      // Step 4
    public GameObject unconsciousSurveyPrefab;        // Step 5
    public GameObject unconsciousMonitorPrefab;       // Step 6

    [Header("Video Clips — CPR")]
    public VideoClip cprCheckBreathingVideo;      // Тыныс алуды тексеру (Кör-Tıŋda-Sez)
    public VideoClip cprRescueBreathsVideo;       // Жасанды тыныс алу аузынан ауызға

    [Header("Video Clips — Choking")]
    public VideoClip chokingHeimlichVideo;        // Геймлих тәсілі (ең күрделі қадам)

    [Header("Video Clips — Bleeding")]
    public VideoClip bleedingTourniquetVideo;     // Жгут салу

    [Header("Video Clips — Unconscious")]
    public VideoClip unconsciousRecoveryVideo;    // Қалпына келтіру позициясы

    IEnumerator Start()
    {
        Application.targetFrameRate = 30;

        if (quizPanel != null) quizPanel.SetActive(false);

        while (ScenarioManager.Instance == null)
        {
            yield return null;
        }

        yield return null;

        // ЖАҢА ТЕКСЕРУ
        if (ScenarioManager.Instance.isCustomScenario)
        {
            if (ScenarioManager.Instance.currentCustomScenario == null)
            {
                Debug.LogError("Custom scenario data is null! Returning to menu.");
                UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
                yield break;
            }
            LoadCustomScenario(ScenarioManager.Instance.currentCustomScenario);
        }
        else
        {
            if (string.IsNullOrEmpty(ScenarioManager.Instance.selectedScenario))
            {
                Debug.LogError("Selected scenario is null or empty! Returning to menu.");
                UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
                yield break;
            }
            LoadScenario(ScenarioManager.Instance.selectedScenario);
        }
    }

    void LoadCustomScenario(CustomScenario customData)
    {
        if (currentModel != null) Destroy(currentModel);
        steps.Clear();
        currentStepIndex = 0;
        titleText.text = customData.scenarioName;

        foreach (var customStep in customData.steps)
        {
            ScenarioStep newStep = new ScenarioStep
            {
                title = customStep.title,
                description = customStep.description,
                information = customStep.description,
                warnings = customStep.warnings,
                modelUrl = customStep.modelUrl
            };

            // Парсим URL видео
            if (!string.IsNullOrWhiteSpace(customStep.videoUrl))
            {
                // ScenarioStep.stepVideoClip — это VideoClip, а не URL.
                // Для сетевого видео используем pendingVideoUrl (нужно добавить в ShowStep).
                // Сохраняем в отдельное поле через наследование — см. ниже.
                newStep.modelUrl = customStep.modelUrl; // modelUrl уже присвоен выше
                                                        // videoUrl хранится прямо в customStep и читается в ShowStep
            }

            // Парсим квиз из сырого текста
            if (!string.IsNullOrWhiteSpace(customStep.quizRaw))
                newStep.quiz = ParseQuiz(customStep.quizRaw);

            steps.Add(newStep);
        }

        ShowStep();
    }

    // Парсер квиза. Формат:
    //   Строка 0: вопрос
    //   Строки 1..N-1: ответы (правильный начинается со *)
    //   Последняя строка (если число): лимит времени
    private QuizData ParseQuiz(string raw)
    {
        string[] lines = raw.Split('\n');
        if (lines.Length < 3) return null; // минимум: вопрос + 2 ответа

        var quiz = new QuizData();
        quiz.question = lines[0].Trim();

        var answers = new System.Collections.Generic.List<string>();
        quiz.correctAnswerIndex = 0;
        quiz.timeLimit = 15f;

        for (int i = 1; i < lines.Length; i++)
        {
            string line = lines[i].Trim();
            if (line == "") continue;

            // Последняя строка — число? Значит это таймер
            if (i == lines.Length - 1 && float.TryParse(line, out float t))
            {
                quiz.timeLimit = t;
                break;
            }

            if (line.StartsWith("*"))
            {
                quiz.correctAnswerIndex = answers.Count;
                answers.Add(line.Substring(1).Trim());
            }
            else
            {
                answers.Add(line);
            }
        }

        quiz.answers = answers.ToArray();
        return (answers.Count >= 2) ? quiz : null;
    }

    void LoadScenario(string scenario)
    {
        if (currentModel != null)
            Destroy(currentModel);

        steps.Clear();
        currentStepIndex = 0;

        switch (scenario)
        {
            case "CPR":
                titleText.text = "CPR - Cardiac Arrest";
                AddCPRSteps();
                break;

            case "Choking":
                titleText.text = "Choking (Adult)";
                AddChokingSteps();
                break;

            case "Bleeding":
                titleText.text = "Severe Bleeding";
                AddBleedingSteps();
                break;

            case "Unconscious":
                titleText.text = "Unconscious Person";
                AddUnconsciousSteps();
                break;

            case "Dynamic":
            case "DynamicFirstAid":
            case "PatientAssessment":
                titleText.text = "Dynamic Patient Assessment";
                AddDynamicPatientAssessmentSteps();
                break;
        }

        ShowStep();
    }

    void ShowStep()
    {
        if (steps.Count == 0 || currentStepIndex >= steps.Count)
            return;

        ScenarioStep step = steps[currentStepIndex];

        // --- ТАЙПИНГ ЭФФЕКТІСІ ҮШІН МАҢЫЗДЫ: Алдыңғы жазылып жатқан мәтіндерді тоқтату ---
        StopAllCoroutines();

        // Тақырып пен ескертуді бірден шығарамыз (олар қысқа)
        if (stepTitleText) stepTitleText.text = step.title;
        if (warningText) warningText.text = "<color=red>" + step.warnings + "</color>";

        // Сипаттама мен толық ақпаратты біртіндеп шығару (Coroutine арқылы)
        if (descriptionText) StartCoroutine(TypeText(descriptionText, step.description));
        if (informationText) StartCoroutine(TypeText(informationText, step.information));

        // --- Кескіндер мен визуализация ---
        if (stepImageUI != null)
        {
            if (step.stepImage != null)
            {
                stepImageUI.sprite = step.stepImage;
                stepImageUI.gameObject.SetActive(true);
            }
            else
            {
                stepImageUI.gameObject.SetActive(false);
            }
        }
        if (videoPanelAnimator != null)
        {
            // Проверяем URL видео для кастомных сценариев
            string customVideoUrl = "";
            if (ScenarioManager.Instance != null && ScenarioManager.Instance.isCustomScenario)
            {
                int idx = currentStepIndex;
                var src = ScenarioManager.Instance.currentCustomScenario.steps;
                if (idx < src.Count)
                    customVideoUrl = src[idx].videoUrl ?? "";
            }

            if (!string.IsNullOrEmpty(customVideoUrl))
                videoPanelAnimator.ShowVideoPanelFromUrl(customVideoUrl);
            else if (step.stepVideoClip != null)
                videoPanelAnimator.ShowVideoPanel(step.stepVideoClip);
            else
                videoPanelAnimator.HideVideoPanel();
        }
        // --- AR Модельдер логикасы ---
        if (currentModel != null)
            Destroy(currentModel);

        pendingPrefab = null;
        pendingModelUrl = null;

        if (!string.IsNullOrEmpty(step.modelUrl))
        {
            pendingModelUrl = step.modelUrl;
            Debug.Log($"[AR] Step '{step.title}': modelUrl = {step.modelUrl}");
        }
        else
        {
            pendingPrefab = step.stepPrefab;
            if (pendingPrefab == null)
                Debug.LogWarning($"[AR] Step '{step.title}': stepPrefab is NULL! Assign it in Inspector.");
            else
                Debug.Log($"[AR] Step '{step.title}': prefab = {pendingPrefab.name} — tap AR plane to place.");
        }

#if UNITY_EDITOR
        // --- EDITOR FALLBACK: автоматически размещаем модель перед камерой ---
        PlaceModelInEditor();
#endif

        // --- Түймелерді басқару ---
        if (prevButton != null)
        {
            prevButton.interactable = (currentStepIndex > 0);
        }

        // --- Тест логикасы ---
        if (step.quiz != null && !string.IsNullOrEmpty(step.quiz.question))
        {
            StartQuiz(step.quiz);
        }
        else
        {
            if (step.patientStateDecision != PatientStateDecision.None)
            {
                StartPatientStateChoice(step.patientStateDecision);
            }
            else if (quizPanel != null)
            {
                quizPanel.SetActive(false);
            }

            isQuizActive = false;
            isPatientStateChoiceActive = step.patientStateDecision != PatientStateDecision.None;
        }
    }
    IEnumerator TypeText(TextMeshProUGUI textUI, string fullText)
    {
        textUI.text = ""; // Мәтінді тазалаймыз
        foreach (char letter in fullText.ToCharArray())
        {
            textUI.text += letter;
            yield return new WaitForSeconds(typingSpeed);
        }
    }
    void StartQuiz(QuizData data)
    {
        isQuizActive = true;
        isPatientStateChoiceActive = false;
        quizPanel.SetActive(true);
        quizQuestionText.text = data.question;
        timeLeft = data.timeLimit;

        for (int i = 0; i < answerButtons.Length; i++)
        {
            if (i < data.answers.Length)
            {
                answerButtons[i].gameObject.SetActive(true);
                answerButtons[i].GetComponentInChildren<TextMeshProUGUI>().text = data.answers[i];

                int index = i;
                answerButtons[i].onClick.RemoveAllListeners();
                answerButtons[i].onClick.AddListener(() => OnAnswerSelected(index));
            }
            else
            {
                answerButtons[i].gameObject.SetActive(false);
            }
        }
    }

    void StartPatientStateChoice(PatientStateDecision decision)
    {
        isQuizActive = false;
        isPatientStateChoiceActive = true;

        if (quizPanel != null) quizPanel.SetActive(true);
        if (timerText != null) timerText.text = "";
        string[] answers;
        PatientStateBranch[] branches;

        switch (decision)
        {
            case PatientStateDecision.EmergencyTypeAssessment:
                if (quizQuestionText != null)
                    quizQuestionText.text = "Что опаснее всего видно сейчас?";

                answers = new string[]
                {
                    "Сильное кровотечение",
                    "Проблема с дыханием",
                    "Без сознания / не дышит"
                };

                branches = new PatientStateBranch[]
                {
                    PatientStateBranch.Bleeding,
                    PatientStateBranch.BreathingProblem,
                    PatientStateBranch.UnconsciousProblem
                };
                break;

            case PatientStateDecision.BreathingProblemAssessment:
                if (quizQuestionText != null)
                    quizQuestionText.text = "Какая именно проблема с дыханием?";

                answers = new string[]
                {
                    "Поперхнулся / давится",
                    "Тяжело дышит",
                    "Нет дыхания / пульса"
                };

                branches = new PatientStateBranch[]
                {
                    PatientStateBranch.Choking,
                    PatientStateBranch.CannotBreathe,
                    PatientStateBranch.NoBreathingNoPulse
                };
                break;

            case PatientStateDecision.UnconsciousProblemAssessment:
                if (quizQuestionText != null)
                    quizQuestionText.text = "Без сознания: дыхание есть?";

                answers = new string[]
                {
                    "Дышит",
                    "Не дышит / нет пульса",
                    "Есть реакция"
                };

                branches = new PatientStateBranch[]
                {
                    PatientStateBranch.UnconsciousBreathing,
                    PatientStateBranch.NoBreathingNoPulse,
                    PatientStateBranch.Responsive
                };
                break;

            default:
                if (quizQuestionText != null)
                    quizQuestionText.text = "Определите состояние пациента";

                answers = new string[]
                {
                    "В сознании / реагирует",
                    "Без сознания, дышит",
                    "Не дышит и нет пульса"
                };

                branches = new PatientStateBranch[]
                {
                    PatientStateBranch.Responsive,
                    PatientStateBranch.UnconsciousBreathing,
                    PatientStateBranch.NoBreathingNoPulse
                };
                break;
        }

        if (answerButtons.Length < answers.Length)
        {
            Debug.LogWarning($"[Dynamic] Need {answers.Length} answer buttons, but only {answerButtons.Length} assigned.");
        }

        for (int i = 0; i < answerButtons.Length; i++)
        {
            if (i < answers.Length)
            {
                answerButtons[i].gameObject.SetActive(true);
                answerButtons[i].GetComponentInChildren<TextMeshProUGUI>().text = answers[i];

                PatientStateBranch selectedBranch = branches[i];
                answerButtons[i].onClick.RemoveAllListeners();
                answerButtons[i].onClick.AddListener(() => ApplyPatientStateBranch(selectedBranch));
            }
            else
            {
                answerButtons[i].gameObject.SetActive(false);
            }
        }

        if (nextButton != null) nextButton.interactable = false;
    }

    void ApplyPatientStateBranch(PatientStateBranch branch)
    {
        isPatientStateChoiceActive = false;
        if (quizPanel != null) quizPanel.SetActive(false);
        if (nextButton != null) nextButton.interactable = true;

        int insertFromIndex = currentStepIndex + 1;
        if (insertFromIndex < steps.Count)
            steps.RemoveRange(insertFromIndex, steps.Count - insertFromIndex);

        switch (branch)
        {
            case PatientStateBranch.Responsive:
                AddResponsivePatientSteps();
                break;

            case PatientStateBranch.UnconsciousBreathing:
                AppendStepsFromScenario(AddUnconsciousSteps, 3);
                break;

            case PatientStateBranch.NoBreathingNoPulse:
                AppendStepsFromScenario(AddCPRSteps, 3);
                break;

            case PatientStateBranch.Bleeding:
                AppendStepsFromScenario(AddBleedingSteps, 0);
                break;

            case PatientStateBranch.Choking:
                AppendStepsFromScenario(AddChokingSteps, 0);
                break;

            case PatientStateBranch.CannotBreathe:
                AddCannotBreatheSteps();
                break;

            case PatientStateBranch.Unconscious:
                AppendStepsFromScenario(AddUnconsciousSteps, 0);
                break;

            case PatientStateBranch.BreathingProblem:
                AddBreathingProblemDecisionSteps();
                break;

            case PatientStateBranch.UnconsciousProblem:
                AddUnconsciousProblemDecisionSteps();
                break;
        }

        currentStepIndex++;
        ShowStep();
    }

    void OnAnswerSelected(int index)
    {
        if (index == steps[currentStepIndex].quiz.correctAnswerIndex)
        {
            // Правильный ответ — убираем панель, разрешаем идти дальше
            isQuizActive = false;
            quizPanel.SetActive(false);
        }
        else
        {
            // Неправильный ответ — возвращаем в начало
            ReturnToStart();
        }
    }

    void ReturnToStart()
    {
        Debug.Log("Неправильный ответ или вышло время. Возврат в начало.");
        isQuizActive = false;
        isPatientStateChoiceActive = false;
        currentStepIndex = 0;
        ShowStep();
    }

#if UNITY_EDITOR
    /// <summary>
    /// Editor-only: размещает модель перед камерой без AR-плоскости.
    /// Вызывается из ShowStep() только в Editor.
    /// </summary>
    void PlaceModelInEditor()
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        // Позиция: 1.5м перед камерой
        Vector3 spawnPos = cam.transform.position + cam.transform.forward * 1.5f;
        Quaternion spawnRot = Quaternion.identity;

        if (!string.IsNullOrEmpty(pendingModelUrl))
        {
            Pose editorPose = new Pose(spawnPos, spawnRot);
            PlaceDownloadedModel(pendingModelUrl, editorPose);
            pendingModelUrl = null;
            Debug.Log("[EDITOR] Модель (URL) автоматически размещена перед камерой.");
        }
        else if (pendingPrefab != null)
        {
            currentModel = Instantiate(pendingPrefab, spawnPos, spawnRot);
            currentModel.transform.localScale = Vector3.one * 0.5f;
            Debug.Log($"[EDITOR] Префаб '{pendingPrefab.name}' автоматически размещён перед камерой.");
            pendingPrefab = null;
        }
    }
#endif

    void Update()
    {
        // --- Логика Таймера Теста ---
        if (isQuizActive)
        {
            timeLeft -= Time.deltaTime;
            if (timerText) timerText.text = Mathf.Ceil(timeLeft).ToString() + "s";

            if (timeLeft <= 0)
            {
                ReturnToStart();
            }
            return; // Пока активен тест, AR взаимодействий нет
        }

        // Если на экране нет касаний, ничего не делаем
        if (isPatientStateChoiceActive)
            return;

#if UNITY_EDITOR
        // --- EDITOR: вращение модели мышью ---
        if (currentModel != null && Input.GetMouseButton(0))
        {
            if (!UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
            {
                float rotationY = -Input.GetAxis("Mouse X") * rotationSpeed * 10f;
                currentModel.transform.Rotate(0, rotationY, 0, Space.Self);
            }
        }
        // В Editor моделі уже размещены автоматически, touch-логика не нужна
#else
        if (Input.touchCount == 0) return;

        Touch touch = Input.GetTouch(0);

        // Игнорируем клики, если палец над кнопкой UI
        if (UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject(touch.fingerId))
            return;

        // --- СИТУАЦИЯ 1: Нужно поставить модель ---
        if (pendingPrefab != null || !string.IsNullOrEmpty(pendingModelUrl))
        {
            if (touch.phase == TouchPhase.Began)
            {
                if (raycastManager.Raycast(touch.position, hits, TrackableType.PlaneWithinPolygon))
                {
                    Pose hitPose = hits[0].pose;

                    // 1.1 Загрузка из интернета (.glb)
                    if (!string.IsNullOrEmpty(pendingModelUrl))
                    {
                        PlaceDownloadedModel(pendingModelUrl, hitPose);
                        pendingModelUrl = null; // Очищаем, чтобы не скачивать повторно
                    }
                    // 1.2 Установка стандартного префаба
                    else if (pendingPrefab != null)
                    {
                        currentModel = Instantiate(pendingPrefab, hitPose.position, hitPose.rotation);
                        currentModel.transform.localScale = Vector3.one * 0.5f;
                        pendingPrefab = null; // Очищаем
                    }
                }
            }
        }
        // --- СИТУАЦИЯ 2: Модель уже на сцене — вращаем её ---
        else if (currentModel != null)
        {
            if (touch.phase == TouchPhase.Moved)
            {
                // Вращение пальцем (ось Y)
                float rotationY = -touch.deltaPosition.x * rotationSpeed;
                currentModel.transform.Rotate(0, rotationY, 0, Space.Self);
            }
        }
#endif
    }

    async void PlaceDownloadedModel(string url, Pose hitPose)
    {
        GameObject modelContainer = new GameObject("DownloadedModel_Container");
        modelContainer.transform.position = hitPose.position;
        modelContainer.transform.rotation = hitPose.rotation;

        modelContainer.transform.localScale = Vector3.one;

        var gltf = new GltfImport();
        bool success = await gltf.Load(url);

        if (success)
        {
            await gltf.InstantiateMainSceneAsync(modelContainer.transform);
            FixMaterials(modelContainer);
            currentModel = modelContainer;

            await System.Threading.Tasks.Task.Yield();

            float targetScale = 0.01f;
            modelContainer.transform.localScale = Vector3.one * targetScale;

            Debug.Log("Модель успешно скачана и отмасштабирована!");
        }
        else
        {
            Destroy(modelContainer);
            Debug.LogError("Ошибка загрузки модели по URL: " + url);
        }
    }

    void FixMaterials(GameObject targetModel)
    {
        Renderer[] renderers = targetModel.GetComponentsInChildren<Renderer>(true);

        Shader defaultShader = Shader.Find("Universal Render Pipeline/Lit");
        if (defaultShader == null) defaultShader = Shader.Find("Standard");

        if (defaultShader != null)
        {
            foreach (Renderer rend in renderers)
            {
                foreach (Material mat in rend.materials)
                {
                    mat.shader = defaultShader;
                }
            }
            Debug.Log("Материалы исправлены.");
        }
    }

    public void NextStep()
    {
        if (isQuizActive || isPatientStateChoiceActive) return; // Блокируем кнопку Next, пока не выбран ответ

        currentStepIndex++;

        if (currentStepIndex >= steps.Count)
        {
            if (videoPanelAnimator != null)
                videoPanelAnimator.HideVideoPanel();

            // Запуск эффекта блёсток при завершении уровня
            if (levelCompleteEffect != null)
                levelCompleteEffect.Play();

            descriptionText.text =
            "Scenario Completed!\n\n" +
            "If you want to practice press Practice CPR.\n" +
            "If you want to see demonstration press Watch CPR.";

            if (informationText) informationText.text = ""; // Очищаем информацию в конце

            nextButton.interactable = false;

            if (ScenarioManager.Instance != null && ScenarioManager.Instance.isCustomScenario)
            {
                practiceButton.SetActive(false);
                watchButton.SetActive(false);
                descriptionText.text += "\nThank you for completing this user-generated scenario.";
            }
            else
            {
                practiceButton.SetActive(true);
                watchButton.SetActive(true);
            }
            return;
        }

        ShowStep();
    }

    public void PrevStep()
    {
        if (currentStepIndex > 0)
        {
            currentStepIndex--;

            if (nextButton != null) nextButton.interactable = true;
            if (practiceButton != null) practiceButton.SetActive(false);
            if (watchButton != null) watchButton.SetActive(false);

            ShowStep();
        }
    }

    void AddDynamicPatientAssessmentSteps()
    {
        steps.Add(new ScenarioStep
        {
            title = "1. Безопасность места",
            description =
                "Осмотрите место происшествия. Подходите к пациенту только если это безопасно.\n" +
                "Проверьте транспорт, огонь, электричество, воду, стекло, агрессивную среду и другие угрозы.",
            information =
                "Динамический сценарий начинается не с готового диагноза, а с оценки ситуации. Пользователь должен понять, какая проблема главная: кровь, поперхивание, нарушение дыхания, потеря сознания или остановка сердца.",
            warnings = "Если место опасно - не подходите к пациенту.",
            stepImage = sceneAssessmentSprite,
            stepPrefab = cprSceneAssessmentPrefab
        });

        steps.Add(new ScenarioStep
        {
            title = "2. Быстрый осмотр пациента",
            description =
                "Посмотрите на пациента и модель: есть ли сильная кровь, признаки удушья, тяжелая одышка или потеря сознания?\n" +
                "Если пациент лежит и не реагирует, откройте дыхательные пути и проверьте дыхание не дольше 10 секунд.",
            information =
                "Главная идея: сначала выбрать самую опасную проблему. Массивное кровотечение останавливают сразу. Если человек давится и не может говорить - это поперхивание. Если дыхания нет или пульс не определяется - нужна СЛР.",
            warnings = "Не тратьте много времени на осмотр. Если дыхания нет - сразу переходите к СЛР.",
            stepImage = safetySprite != null ? safetySprite : sceneAssessmentSprite,
            stepPrefab = cprSceneAssessmentPrefab
        });

        steps.Add(new ScenarioStep
        {
            title = "3. Выберите ветку помощи",
            description =
                "Нажмите кнопку, которая лучше всего описывает состояние пациента.\n" +
                "После выбора сценарий автоматически продолжится по нужной ветке.",
            information =
                "Кровотечение -> давление/тампонада/жгут. Поперхивание -> кашель, удары по спине, прием Геймлиха. Не может дышать -> посадить, вызвать помощь, убрать триггер, контролировать состояние. Без сознания -> дыхательные пути, дыхание, восстановительное положение или СЛР. Нет дыхания/пульса -> 112 и СЛР.",
            warnings = "Если сомневаетесь между 'без сознания' и 'нет дыхания/пульса', выбирайте 'нет дыхания/пульса'.",
            stepImage = checkBreathingSprite != null ? checkBreathingSprite : responsiveCheckSprite,
            stepPrefab = cprCheckBreathingPrefab != null ? cprCheckBreathingPrefab : cprResponsiveCheckPrefab,
            stepVideoClip = cprCheckBreathingVideo,
            patientStateDecision = PatientStateDecision.EmergencyTypeAssessment
        });
    }

    void AddCannotBreatheSteps()
    {
        steps.Add(new ScenarioStep
        {
            title = "Тяжелое нарушение дыхания",
            description =
                "Пациент в сознании, но ему трудно дышать: он хватает воздух, не может говорить длинными фразами, бледнеет или синеет.\n" +
                "Посадите его полусидя. Ослабьте тесную одежду и обеспечьте доступ воздуха.",
            information =
                "Эта ветка нужна для ситуации, когда человек не поперхнулся, но дыхание резко нарушено: приступ астмы, аллергическая реакция, паника, боль в груди, дым, химический раздражитель. Главная задача - облегчить дыхание и быстро вызвать помощь.",
            warnings = "Не укладывайте пациента на спину, если ему легче сидеть.",
            stepImage = checkBreathingSprite,
            stepPrefab = cprCheckBreathingPrefab
        });

        steps.Add(new ScenarioStep
        {
            title = "Вызовите помощь",
            description =
                "Позвоните 112/103 или попросите конкретного человека сделать это.\n" +
                "Сообщите: пациент в сознании, но не может нормально дышать.",
            information =
                "Если рядом есть назначенный пациенту ингалятор, автоинъектор адреналина или другое личное средство, помогите ему воспользоваться им по инструкции. Не давайте чужие лекарства.",
            warnings = "Если есть отек лица/губ/языка, сыпь и быстрое ухудшение - подозревайте аллергию и срочно вызывайте помощь.",
            stepImage = callEmergencySprite,
            stepPrefab = cprCallEmergencyPrefab
        });

        steps.Add(new ScenarioStep
        {
            title = "Контроль состояния",
            description =
                "Постоянно наблюдайте за сознанием, цветом кожи и дыханием.\n" +
                "Если пациент потерял сознание - проверьте дыхание. Если дыхания нет или оно ненормальное, начинайте СЛР.",
            information =
                "Эта ветка может перейти в ветку 'без сознания' или 'СЛР', если состояние ухудшилось. В учебной сцене пользователь должен понять момент, когда простое наблюдение уже недостаточно.",
            warnings = "При остановке дыхания не ждите скорую - начинайте СЛР.",
            stepImage = monitoringSprite != null ? monitoringSprite : checkBreathingSprite,
            stepPrefab = unconsciousMonitorPrefab != null ? unconsciousMonitorPrefab : cprCheckBreathingPrefab,
            quiz = new QuizData
            {
                question = "Что делать, если пациент с одышкой потерял сознание и не дышит нормально?",
                answers = new string[] { "Дать воды", "Начать СЛР", "Посадить обратно" },
                correctAnswerIndex = 1,
                timeLimit = 15f
            }
        });
    }

    void AddBreathingProblemDecisionSteps()
    {
        steps.Add(new ScenarioStep
        {
            title = "Уточните дыхательную проблему",
            description =
                "Посмотрите на пациента: он подавился предметом или просто тяжело дышит?\n" +
                "Если человек держится за горло, не может говорить/кашлять и синеет - выбирайте поперхивание.\n" +
                "Если предмета нет, но есть сильная одышка, свист, аллергия, дым или боль в груди - выбирайте тяжелое дыхание.",
            information =
                "Поперхивание и тяжелая одышка требуют разных действий. При поперхивании главная цель - убрать инородное тело. При тяжелой одышке без инородного тела - посадить пациента, вызвать помощь, убрать триггер и наблюдать. Если дыхание исчезло или пульса нет, это уже ветка СЛР.",
            warnings = "Если пациент потерял сознание или перестал нормально дышать - переходите к СЛР.",
            stepImage = checkBreathingSprite != null ? checkBreathingSprite : chokingAssessmentSprite,
            stepPrefab = cprCheckBreathingPrefab != null ? cprCheckBreathingPrefab : chokingAssessPrefab,
            patientStateDecision = PatientStateDecision.BreathingProblemAssessment
        });
    }

    void AddUnconsciousProblemDecisionSteps()
    {
        steps.Add(new ScenarioStep
        {
            title = "Уточните состояние без сознания",
            description =
                "Откройте дыхательные пути и проверьте дыхание не дольше 10 секунд.\n" +
                "Если грудная клетка регулярно поднимается и воздух ощущается - пациент без сознания, но дышит.\n" +
                "Если дыхания нет, оно редкое судорожное, или пульс не определяется - выбирайте СЛР.",
            information =
                "Эта точка решает главную развилку: восстановительное положение при нормальном дыхании или немедленная СЛР при отсутствии нормального дыхания. Агональные редкие вдохи не считаются нормальным дыханием.",
            warnings = "Сомневаетесь в дыхании - выбирайте СЛР.",
            stepImage = unconsciousBreathingSprite != null ? unconsciousBreathingSprite : checkBreathingSprite,
            stepPrefab = unconsciousBreathCheckPrefab != null ? unconsciousBreathCheckPrefab : cprCheckBreathingPrefab,
            stepVideoClip = cprCheckBreathingVideo,
            patientStateDecision = PatientStateDecision.UnconsciousProblemAssessment
        });
    }

    void AddResponsivePatientSteps()
    {
        steps.Add(new ScenarioStep
        {
            title = "Пациент реагирует",
            description =
                "Оставьте пациента в удобном безопасном положении. Узнайте, что случилось, где болит, есть ли кровотечение или другие опасные симптомы.\n" +
                "При ухудшении состояния снова проверьте сознание и дыхание.",
            information =
                "Если есть сильная боль, травма, кровотечение, одышка, слабость, спутанность сознания или состояние ухудшается - вызовите 112/103 и наблюдайте за пациентом до приезда помощи.",
            warnings = "Не давайте еду, воду или лекарства, если причина состояния не ясна.",
            stepImage = monitoringSprite != null ? monitoringSprite : sceneAssessmentSprite,
            stepPrefab = unconsciousMonitorPrefab != null ? unconsciousMonitorPrefab : cprSceneAssessmentPrefab
        });
    }

    void AppendStepsFromScenario(Action addSteps, int startIndex)
    {
        List<ScenarioStep> targetSteps = steps;
        steps = new List<ScenarioStep>();
        addSteps();

        List<ScenarioStep> sourceSteps = steps;
        steps = targetSteps;

        for (int i = startIndex; i < sourceSteps.Count; i++)
            steps.Add(sourceSteps[i]);
    }

    void AddCPRSteps()
    {
        steps.Add(new ScenarioStep
        {
            title = "Қауіпсіздікті қамтамасыз ету",
            description =
                "Оқиға орны сіз үшін және зардап шегуші үшін қауіпсіз екеніне көз жеткізіңіз.\n" +
                "Қауіптердің бар-жоғын тексеріңіз: электр тогы, көлік қозғалысы, газ немесе су.",
            information =
                "Зардап шегушіге жақындамас бұрын, оқиға орнының қауіпсіз екеніне көз жеткізу керек. Алғашқы көмек көрсетудің халықаралық ұсынымдарына сәйкес, құтқарушы екінші құрбанға айналмауы тиіс. Бірнеше секундқа тоқтап, өзіңіздің және зардап шегушінің айналасын мұқият тексеріңіз. Қауіпті факторларды тексеріңіз: жол қозғалысы, өрт, түтін, газдың шығуы, ашық электр сымдары, электр көзінің жанындағы су, конструкциялардың құлауы, шыны, өткір заттар, агрессивті жануарлар немесе адамдар.\n\nЕгер оқиға жолда болса, алдымен көліктің тоқтағанына немесе қауіпсіз қашықтықта екеніне көз жеткізіңіз. Егер жақын жерде өрт, газ иісі немесе химиялық заттар болса, қауіп жойылмайынша зардап шегушіге жақындамаңыз. Электр жарақаты күдігі болса, ток көзі өшірілмейінше адамға ешқашан қол тигізбеңіз.\n\nҚан, құсық немесе басқа биологиялық сұйықтықтар болған жағдайда, мүмкіндігінше медициналық қолғап, маска немесе кез келген қорғаныс кедергісін қолданыңыз.",
            warnings =
                "ЕШҚАШАН екінші құрбан болмаңыз.\nЕгер орын қауіпті болса — жақындамаңыз.",
            stepImage = sceneAssessmentSprite,
            stepPrefab = cprSceneAssessmentPrefab
            // видео нет — панель скрыта
        });

        steps.Add(new ScenarioStep
        {
            title = "Реакцияны тексеру",
            description =
                "Дауыстап: 'Сізге көмек керек пе?' — деп сұраңыз.\n" +
                "Иығынан ақырын сілкіңіз.\n" +
                "Реакцияның бар-жоғын тексеріңіз (ыңырсу, көз ашу, қозғалыс).",
            information =
                "Оқиға орны қауіпсіз деп танылғаннан кейін, адамның есін білетінін тез арада тексеру қажет. Зардап шегушіге бас жағынан немесе иық тұсынан жақындаңыз. Бұл ол көзін ашқан жағдайда сізді көруіне мүмкіндік береді және мойынның кездейсоқ қозғалу қаупін азайтады.\n\nАдамға дауыстап тіл қатыңыз: «Мені естисіз бе? Сізге көмек керек пе? Не болды?». Сонымен қатар, қолыңызды оның иығына ақырын қойып, сәл сілкіңіз. Басын шайқамаңыз және мойнын бүкпеңіз, себебі зардап шегушінің омыртқасы зақымдалуы мүмкін.\n\nКез келген реакцияны бағалаңыз. Егер ешқандай реакция болмаса, адамды дереу ес-түссіз деп есептеу керек. Тексеруге 5–10 секундтан артық уақыт жұмсамаңыз.",
            warnings =
                "Басын ШАЙҚАМАҢЫЗ — мойын жарақаты болуы мүмкін.\nБұған 5-10 секундтан артық уақыт жұмсамаңыз.",
            stepImage = responsiveCheckSprite,
            stepPrefab = cprResponsiveCheckPrefab
            // видео нет
        });

        // ★ ВИДЕО 1 — Проверка дыхания (Look-Listen-Feel)
        steps.Add(new ScenarioStep
        {
            title = "Тыныс алуды тексеру",
            description =
                "Басын артқа шалқайтып, иегін көтеріңіз.\n" +
                "Құлағыңызды ерніне жақындатып, кеуде қуысына қараңыз ('Естимін, Көремін, Сеземін' әдісі).\n" +
                "Қалыпты тыныс алуды іздеңіз.",
            information =
                "Ес-түссіз жатқан адамның тілі артқа кетіп, тыныс алу жолдарын жауып қалуы мүмкін. Сондықтан тыныс алуды тексермес бұрын тыныс алу жолдарын ашу керек. Ол үшін бір қолыңызды зардап шегушінің маңдайына қойып, екінші қолыңыздың екі саусағымен иегін ақырын жоғары көтеріңіз. Басы сәл артқа шалқаюы керек. Бұл әдіс «басты шалқайту және иекті көтеру» деп аталады.\n\nТыныс алу жолдарын ашқаннан кейін құлағыңызды зардап шегушінің аузы мен мұрнына жақындатып, сонымен бірге оның кеуде қуысына қараңыз. 10 секундтан асырмай «Естимін, Көремін, Сеземін» ережесін қолданыңыз.\n\nЕСТИМІН — дем алу мен дем шығару дыбысы естіле ме, тыңдаңыз.\nКӨРЕМІН — кеуде қуысының көтеріліп-түскенін бақылаңыз.\nСЕЗЕМІН — ауа ағынын бетіңізбен сезінуге тырысыңыз.\n\nСирек, шулы, құрысулы тыныс алу қалыпты болып саналмайды — агониялық тыныс алу деп аталады. Егер тыныс болмаса немесе күмәнді болса — дереу ӨЖР бастаңыз.",
            warnings =
                "Агониялық тыныс алу (сирек құрысулы тыныс) — бұл қалыпты ЕМЕС.\nЕгер тыныс болмаса немесе күмәнді болса — ӨЖР бастаңыз.",
            stepImage = checkBreathingSprite,
            stepPrefab = cprCheckBreathingPrefab,
            stepVideoClip = cprCheckBreathingVideo    // ★ ВИДЕО 1
        });

        steps.Add(new ScenarioStep
        {
            title = "Көмек шақыру және АНД алу",
            description =
                "112 (немесе жергілікті шұғыл қызмет нөміріне) қоңырау шалыңыз.\n" +
                "Нақты мекенжайды және жағдайды (ес-түссіз, дем алмайды) айтыңыз.\n" +
                "Айналадағылардан АНД (AED) аппаратын әкелуді дауыстап сұраңыз.",
            information =
                "Адамның ес-түссіз екені және қалыпты тыныс алмайтыны анықталған бойда, дереу шұғыл қызметтерді шақыру қажет. Қазақстанда 112 немесе 103 нөміріне қоңырау шалу керек.\n\nЕгер сіз жалғыз болсаңыз, диспетчермен сөйлесу және реанимацияны бастау үшін телефонның дауыс зорайтқышын (спикер) пайдаланыңыз. Диспетчерге сабырлы және анық дауыспен хабарлаңыз: нақты мекенжай, зардап шегушінің жасы (белгілі болса), адамның ес-түссіз екені және дем алмайтыны.\n\nЕгер қасыңызда басқа адамдар болса, нақты бір адамға жүгініңіз: «Сіз, 112-ге хабарласыңыз». «Сіз, АНД аппаратын әкеліңіз».",
            warnings =
                "Қолыңыз бос болуы үшін телефонның дауыс зорайтқышын қосыңыз.",
            stepImage = callEmergencySprite,
            stepPrefab = cprCallEmergencyPrefab
            // видео нет
        });

        steps.Add(new ScenarioStep
        {
            title = "Кеуде қуысын қысу",
            description =
                "Алақанның негізін кеуденің ортасына қойыңыз. Екінші қолды үстіне қойып, саусақтарды айқастырыңыз.\n" +
                "Тереңдігі: қатаң түрде 5–6 см.\n" +
                "Қарқыны: минутына 100–120 рет",
            information =
                "30 компрессиядан кейін 2 рет жасанды тыныс алу керек. Жасанды тыныс алу зардап шегушінің өкпесіне оттегін жеткізуге көмектеседі. Алайда, тыныс алудың тиімділігі тыныс алу жолдарының ашық болуына байланысты. Сондықтан алдымен басын қайтадан шалқайтып, иегін көтеріңіз.\n\nЗардап шегушінің маңдайындағы қолыңыздың саусақтарымен мұрнын қысыңыз. Шамамен 1 секунд ішінде баяу дем шығарыңыз. Кеуде қуысын бақылаңыз: ол сәл көтерілуі керек. Бірінші демнен кейін кеуде қуысының төмен түсуін күтіп, екінші демді қайталаңыз. Содан кейін бірден компрессияға оралыңыз. Екі дем алуға арналған үзіліс 10 секундтан аспауы керек.",
            warnings =
                "Әрбір басудан кейін кеуде қуысының толық жазылуына мүмкіндік беріңіз.\nКомпрессиялар арасындағы үзілістерді азайтыңыз.",
            stepImage = chestCompressionsSprite,
            stepPrefab = cprChestCompressionsPrefab,
            enableBodyTracking = true,
            quiz = new QuizData
            {
                question = "ӨЖР кезінде басу қарқыны қандай болуы керек?",
                answers = new string[] { "минутына 60-80", "минутына 100-120", "минутына 150-ден көп" },
                correctAnswerIndex = 1,
                timeLimit = 15f
            }
            // видео нет — движения объясняет 3D-модель + body tracking
        });

        // ★ ВИДЕО 2 — Искусственное дыхание рот-в-рот
        steps.Add(new ScenarioStep
        {
            title = "Жасанды тыныс алу (30:2)",
            description =
                "30 рет басудан кейін 2 рет 'ауыздан ауызға' дем салыңыз.\n" +
                "Мұрнын қысып, аузын ерніңізбен тығыз жабыңыз.\n" +
                "Дем шығару кеуде көтерілгенше 1 секундқа созылады.",
            information =
                "Автоматты сыртқы дефибриллятор жүрек ырғағының қауіпті бұзылуларын анықтауға арналған және жүректің қалыпты жұмысын қалпына келтіретін электр зарядын бере алады. Қазіргі заманғы дефибрилляторлар арнайы медициналық білімі жоқ адамдар да қолдана алатындай етіп жасалған.\n\nДефибриллятор жаныңызға келген бойда оны бірден қосыңыз. Зардап шегушінің кеуде қуысын ашыңыз. Бірінші электродты оң жақ бұғана астына жапсырыңыз. Екіншісін — кеуде қуысының сол жақ бүйіріне, қолтық астынан төменірек қойыңыз. Талдау кезінде ешкім зардап шегушіге қол тигізбеуі керек.\n\nЕгер сізде қорғаныс маскасы болмаса немесе жасанды тыныс алуды жүргізуге үйретілмеген болсаңыз, ресми ұсынымдар жедел жәрдем келгенше тек кеуде қуысын үздіксіз қысуды жүргізуге рұқсат береді.",
            warnings =
                "Егер қорғаныс маскасы болмаса немесе білмесеңіз — ТЕК басуды орындаңыз.\nДем алуға арналған үзіліс 10 секундтан аспауы тиіс.",
            stepImage = rescueBreathsSprite,
            stepPrefab = cprRescueBreathsPrefab,
            stepVideoClip = cprRescueBreathsVideo     // ★ ВИДЕО 2
        });

        steps.Add(new ScenarioStep
        {
            title = "АНД қолдану",
            description =
                "АНД әкелінген бойда — оны қосыңыз.\n" +
                "Аппараттың дауыстық нұсқауларын орындаңыз.\n" +
                "Электродтарды жалаңаш, құрғақ кеудеге жапсырыңыз.",
            information =
                "АНД оқиға орнына жеткізілген бойда, оны дереу қосыңыз және құрылғының дауыстық нұсқауларын қатаң орындаңыз. Зардап шегушінің кеудесін ашыңыз. Егер ол су болса — құрғатып сүртіңіз. Егер қалың түк болса — электродтарды жапсыратын жерлерді қырыңыз. Электродтарды дәл электродтардың өзіндегі суреттерде көрсетілгендей жапсырыңыз: біреуі — оң жақ бұғана астына, екіншісі — сол жақ бүйірге, қолтық астынан сәл төмен. Дауыстап: «Бәріңіз алыстаңыздар, пациентке тиіспеңіздер!» — деп бұйрық беріңіз.",
            warnings =
                "Ырғақты талдау және заряд кезінде пациентке ТИІСПЕҢІЗ.\nЗарядтан кейін бірден ӨЖР-ды жалғастырыңыз.",
            stepImage = aedSprite,
            stepPrefab = cprAEDPrefab
            // видео нет
        });
    }

    // =====================================================================
    //  AddChokingSteps() — 1 видео на Heimlich (самый сложный шаг)
    // =====================================================================

    void AddChokingSteps()
    {
        steps.Add(new ScenarioStep
        {
            title = "Ауырлықты бағалау",
            description =
                "Дауыстап сұраңыз: 'Тұншығып жатырсыз ба? Сөйлей аласыз ба?'\n" +
                "Тұншығудың жалпыға мәлім белгісін іздеңіз: екі қол тамақты ұстап тұр.\n" +
                "Шешіңіз: жартылай бітелу (жөтеле алады) немесе толық (дыбыссыз, көгеріп барады).",
            information =
                "Дереу әрекет етіңіз — толық бітелген тыныс жолы 4 минут ішінде миға зақым келтіреді.\n\n" +
                "ЖАРТЫЛАЙ БІТЕЛУ — адам жөтеле, жыла немесе дыбыс шығара алады:\n" +
                "Оны күшпен жөтелуді жалғастыруға ынталандырыңыз. Жөтелу тыныс жолдарында ең жоғары қысым жасайды " +
                "және бөгде заттарды шығарудың ең тиімді жолы болып табылады. Қасында болыңыз, " +
                "әр жөтелге бағыт беріңіз. Тиімді жөтеле алатын кезде арқасына СОҚПАҢЫЗ — " +
                "затты тереңірек итеруі мүмкін. Су БЕРМЕҢІЗ.\n\n" +
                "ТОЛЫҚ БІТЕЛУ — дыбыс жоқ, жөтел әлсіз немесе жоқ, цианоз (ерін/саусақ ұштары көгереді):\n" +
                "Бұл өмірге қауіп төндіретін жағдай. Дереу 112-ге қоңырау шалыңыз немесе " +
                "бөгде адамға шалдыруды тапсырып, өзіңіз арқаға соқпаларды бастаңыз.\n\n" +
                "АРНАЙЫ ЖАҒДАЙЛАР:\n" +
                "• Нәрестелер (1 жасқа дейін): 5 арқа соқпасы + 5 кеуде итерісін қолданыңыз (іш итерісін ЕМЕС).\n" +
                "• Жүкті немесе семіз ересектер: іш итерісін кеуде итерісімен алмастырыңыз.\n" +
                "• Жәбірленуші жалғыз болса: қатты орындық арқасына немесе үстел жиегіне іш тіреңіз.",
            warnings =
                "Адам күшпен жөтеліп жатса КЕДЕРГІ ЖАСАМАҢЫЗ — жөтелуіне мүмкіндік беріңіз.\n" +
                "Соқыр саусақ тазалауға ТЫРЫСПАҢЫЗ — затты тереңірек итеруіңіз мүмкін.",
            stepImage = chokingAssessmentSprite,
            stepPrefab = chokingAssessPrefab
            // видео нет
        });

        steps.Add(new ScenarioStep
        {
            title = "5 Арқа соқпасы",
            description =
                "Жәбірленушінің бүйіріне және сәл артына тұрыңыз.\n" +
                "Бір қолмен кеудесін ұстаңыз; оны алға қарай жақсылап еңкейтіңіз.\n" +
                "Жауырындар арасына алақанның негізімен 5 рет қатты ұрыңыз.\n",
            information =
                "Арқа соқпасының механизмі:\n" +
                "Жәбірленушіні алға еңкейту ауырлық күшінің кез келген ығысқан затты трахеяға кері кетпей " +
                "аузынан түсуіне көмектесуіне мүмкіндік береді. Алақанның негізі " +
                "кішкентай аймаққа күш шоғырландырып, кеудеде күшті қысым толқынын жасайды.\n\n" +
                "ОРЫНДАУ ЖОЛЫ:\n" +
                "1. Жәбірленушінің бүйіріне тұрыңыз, тұрақтылық үшін бір аяқты алға қойыңыз.\n" +
                "2. Үстем емес қолыңызды жәбірленушінің төс сүйегіне (кеудесіне) тіреп ұстаңыз.\n" +
                "3. Оны мүмкіндігінше алға еңкейтіңіз — мұрны кеудесінен төмен болуы керек.\n" +
                "4. Жауырындар арасына (T3–T5 омыртқа деңгейінде) қатты ұрыңыз. Ортадан ұрыңыз.\n" +
                "5. Әр соқпадан кейін аузына қараңыз. Затты көрсеңіз, алыңыз.\n\n" +
                "5 соқпаның барлығы нәтижесіз болса, дереу іш итерісіне (Геймлих тәсілі) өтіңіз.\n" +
                "Кезектестіру: 5 арқа соқпасы → 5 итеріс → 5 арқа соқпасы → … шешілгенше немесе жәбірленуші жығылғанша.",
            warnings =
                "Әлі де күшпен жөтеліп жатқан адамға арқадан СОҚПАҢЫЗ.\n" +
                "Оның АЛҒА еңкейгеніне көз жеткізіңіз — тік тұрғанда соқу затты тереңірек итеруі мүмкін.",
            stepImage = backBlowsSprite,
            stepPrefab = chokingBackBlowsPrefab
            // видео нет
        });

        // ★ ВИДЕО — Heimlich (рука, положение кулака — важно видеть правильно)
        steps.Add(new ScenarioStep
        {
            title = "Іш итерісі (Геймлих тәсілі)",
            description =
                "Жәбірленушінің артына тұрыңыз; екі қолыңызды беліне орап алыңыз.\n" +
                "Жұдырық жасаңыз — бас бармақ жағы іштің ортасына, кіндік пен қабырға арасына тиеді.\n" +
                "Жұдырықты екінші қолыңызбен ұстаңыз.\n" +
                "5 рет ішке және жоғары қарай күшті итеріс жасаңыз (Ж-қозғалыс).\n" +
                "Әр итерістен кейін аузын тексеріңіз.",
            information =
                "Геймлих тәсілі диафрагманы жылдам қысып, ауа ағынын " +
                "трахея арқылы жоғары итеру арқылы жұмыс істейді. Бұл жасанды жөтел бөгде затты шығаруға жеткілікті қысым жасайды.\n\n" +
                "ҚОЛ ОРНЫНЫҢ ДӘЛДІГІ маңызды:\n" +
                "• Тым жоғары (қабырғалар немесе семсер тәрізді өсінді үстінде): қабырға сынуы, бауыр жарылуы қаупі.\n" +
                "• Тым төмен (кіндіктен төмен): тиімсіз — диафрагманы емес, ішектерді қысасыз.\n" +
                "• Дұрысы: кіндіктен екі саусақ жоғары, ортадан.\n\n" +
                "ТЕХНИКА:\n" +
                "1. Үстем қолыңызбен жұдырық жасаңыз.\n" +
                "2. Бас бармақ буынының тегіс жағын іштің ортасына тіреңіз.\n" +
                "3. Жұдырықты екінші қолыңызбен толығымен жабыңыз.\n" +
                "4. Жұдырықты ІШКЕ (омыртқаға қарай) және сонымен қатар ЖОҒАРЫ итеріңіз — Ж-тәрізді қозғалыс.\n" +
                "5. Әр итеріс ұзаққа созылған қысу емес, нақты, күшті қозғалыс болуы керек.\n\n" +
                "ЖҮКТІ НЕМЕСЕ СЕМІЗ ЖӘБІРЛЕНУШІЛЕР ҮШІН:\n" +
                "• КЕУДЕ ИТЕРІСІН қолданыңыз: жұдырықты төс сүйегінің ортасына қойып, ішке қарай күшті итеріңіз.",
            warnings =
                "Қабырғаларға немесе семсер тәрізді өсіндіге ЕШҚАШАН қол тигізбеңіз — ішкі жарақат қаупі бар.\n" +
                "Жүкті немесе семіз науқастар үшін: КЕУДЕ ИТЕРІСІНЕ ауысыңыз.",
            stepImage = heimlichSprite,
            stepPrefab = chokingHeimlichPrefab,
            stepVideoClip = chokingHeimlichVideo,     // ★ ВИДЕО
            quiz = new QuizData
            {
                question = "Іш итерісінде жұдырықты дәл қай жерге қою керек?",
                answers = new string[]
                {
                "Төс сүйегінің ортасына",
                "Кіндік пен төменгі қабырғалар арасына, ортаға",
                "Кіндіктің дәл астына"
                },
                correctAnswerIndex = 1,
                timeLimit = 15f
            }
        });

        steps.Add(new ScenarioStep
        {
            title = "Аузын тексеру",
            description =
                "Әр итеріс топтамасынан кейін аузын кең ашыңыз.\n" +
                "Затты іздеңіз — тек анық көрінсе ғана алыңыз.\n" +
                "Соқыр саусақ тазалауды ЕШҚАШАН жасамаңыз.",
            information =
                "Әр цикл (5 арқа соқпасы + 5 итеріс) өткен соң жалғастырмас бұрын аузын тексеруіңіз керек.\n\n" +
                "АУЗЫН ҚАЛАЙ АШУ КЕРЕК:\n" +
                "Бас бармағыңызды тіліне, сұқ саусағыңызды иегінің астына қойыңыз — 'тіл-жақ көтергіш' тәсілі. " +
                "Бұл тілді алға тартып, тамақтың артынан алшақтатады және көру мүмкіндігіңізді жақсартады.\n\n" +
                "СОҚЫР САУСАҚ ТАЗАЛАУ — НЕГЕ ЕМЕС:\n" +
                "Затты көрмей саусақ енгізу барлық жас топтарында қауіпті:\n" +
                "• Ересектерде: жұмсақ затты (тамақ, сағыз) жұтқыншаққа итеруіңіз мүмкін.\n" +
                "• Нәрестелер мен балаларда: тыныс жолдары кішірек болғандықтан қауіп одан да жоғары.\n" +
                "Алдымен қараңыз — тек зат көрінсе ғана тазалаңыз.",
            warnings =
                "Соқыр саусақ тазалауды ЕШҚАШАН жасамаңыз — затты тереңірек итеруіңіз мүмкін.\n" +
                "Затты ТЕК анық көрінсе ғана алыңыз.",
            stepImage = chokingAssessmentSprite,
            stepPrefab = chokingFingerSweepPrefab
            // видео нет
        });

        steps.Add(new ScenarioStep
        {
            title = "Жәбірленуші жығылды → ӨЖР бастаңыз",
            description =
                "Жәбірленушіні абайлап еденге жатқызыңыз — басын қорғаңыз.\n" +
                "Егер жасалмаса, дереу 112-ге қоңырау шалыңыз.\n" +
                "ӨЖР бастаңыз: 30 кеуде қысымы → аузын ашып қараңыз → 2 жасанды тыныс.\n" +
                "Әр жасанды тыныстан бұрын аузын тексеріп, көрінетін заттарды тазалаңыз.",
            information =
                "Ұзаққа созылған гипоксия есін жоғалтуға себеп болады. Жәбірленуші жығылғанда, " +
                "бүкіл денедегі — тамақтағы да — бұлшықет тонусы босаңсиды. Бұл босаңсу кейде " +
                "жартылай кіріккен затты ығыстыруға мүмкіндік береді, ал кеуде қысымдары оны шығаруға жеткілікті қысым жасайды.\n\n" +
                "ТҰНШЫҚҚАН ЖӘБІРЛЕНУШІ ҮШІН ӨЗГЕРТІЛГЕН ХАТТАМА:\n" +
                "Әр жасанды тыныстан бұрын:\n" +
                "• Аузын кең ашыңыз.\n" +
                "• Затты іздеңіз — көрінсе, ілмекті саусақ тазалаумен алыңыз.\n" +
                "• Дем салуға тырысыңыз. Кірмесе, басты қайта орналастырып, тағы бір рет тырысыңыз.\n\n" +
                "Кеуде қысымдары: қарқыны 100–120/мин, тереңдігі 5–6 см, кеудені толық жазу.\n" +
                "ӨЖР жалғастырыңыз: зат шығып, тыныс алу жанданғанша, жедел жәрдем келгенше немесе АНД дайын болғанша.",
            warnings =
                "Жәбірленушіні жалғыз ҚАЛДЫРМАҢЫЗ.\n" +
                "Аузын әр жасанды тыныстан БҰРЫН тексеріңіз — кейін емес.",
            stepImage = chokingCollapseSprite,
            stepPrefab = chokingCollapsesPrefab
            // видео нет
        });
    }

    // =====================================================================
    //  AddBleedingSteps() — 1 видео на наложение жгута
    // =====================================================================

    void AddBleedingSteps()
    {
        steps.Add(new ScenarioStep
        {
            title = "Тікелей қысым",
            description =
                "Қолда бар ең таза матаны жараның үстіне тікелей қойыңыз.\n" +
                "Екі қолмен немесе дене салмағымен барынша күшпен қысыңыз.\n" +
                "Суланған таңғышты АЛМАҢЫЗ; үстіне тағы қосып, күштірек қысыңыз.\n" +
                "Үздіксіз қысымды кемінде 10 минут ұстаңыз.",
            information =
                "Масивті қан кету 2–3 минут ішінде өлімге әкелуі мүмкін. Бұл қадамға дейінгі барлық әрекет екінші орында.\n\n" +
                "ҮЗДІКСІЗ ҚЫСЫМ НЕГЕ ҚАЖЕТ:\n" +
                "Қан кетіп жатқан тамырды қысқанда, оның ішкі қуысы жабылып, тромбоциттер тығын түзуіне мүмкіндік береді. " +
                "Таңғышты — тіпті қысқа мерзімге — көтеру сол түзілген тығынды бүлдіріп, процесті қайтарады.\n\n" +
                "ТАҢҒЫШ МАТЕРИАЛДАРЫ (қолданылу тәртібімен):\n" +
                "1. Коммерциялық гемостатикалық дәке (каолин немесе хитозанмен сіңдірілген).\n" +
                "2. Алғашқы көмек жинағынан стерильді дәке.\n" +
                "3. Кез келген таза, сіңіргіш мата: бүктелген жейде, мата бөз, ас үй сүлгісі.\n\n" +
                "КІРГЕН ЗАТТАР (пышақ, шыны, металл):\n" +
                "Оларды АЛМАҢЫЗ. Затты айналдыра таңғыш қойыңыз ('донут' сақина тәрізді).",
            warnings =
                "Суланған таңғышты ЕШҚАШАН алмаңыз — түзіліп жатқан ұйытқыны бүлдіресіз.\n" +
                "Кірген заттарды (пышақ, шыны, металл) ЕШҚАШАН алмаңыз.",
            stepImage = directPressureSprite,
            stepPrefab = bleedingDirectPressurePrefab
            // видео нет
        });

        steps.Add(new ScenarioStep
        {
            title = "Жараны тығыздау",
            description =
                "Шап, қолтық немесе мойын аймағындағы терең жараларда тығыздауды қолданыңыз — жгут қоюға болмайтын жерлер.\n" +
                "Жара ішіндегі қан кету нүктесін табыңыз.\n" +
                "Дәкені ең терең нүктеден бетіне қарай, саусақпен-саусақ тығыз тығыңыз.\n" +
                "Үстіне кемінде 3 минут қатты тікелей қысым жасаңыз.",
            information =
                "БУЫН ЖАРАЛАРЫ мен МОЙЫН ЖАРАЛАРЫ: жгутты қан кету нүктесіне жақынырақ қою мүмкін емес. " +
                "Жараны тығыздау (тампонада) мұнда негізгі шара болып табылады.\n\n" +
                "ТЕХНИКА:\n" +
                "1. Жараны толық ашыңыз — қажет болса киімді кесіңіз.\n" +
                "2. Бар болса гемостатикалық дәкені қолданыңыз. Болмаса, стандартты дәкені қолданыңыз.\n" +
                "3. Жара ішіндегі ҚАН КЕТУдің ҚАЙНАРЫН табыңыз.\n" +
                "4. Дәкені тікелей қайнарға итеріңіз. Үстіне қосымша дәке бүктеп тығыңыз.\n" +
                "5. Жара беткейге дейін толық толғанша жалғастырыңыз.\n" +
                "6. Үстіне кемінде 3 минут қатты тікелей қысым жасаңыз.\n\n" +
                "ТЫҒЫЗДАУДАН КЕЙІН БАЙЛАУ:\n" +
                "Тасымалдау кезінде қысымды сақтау үшін тығыздалған дәкенің үстіне қысымды таңғыш байлаңыз.",
            warnings =
                "Кеуде немесе іш жараларын ТЫҒЫЗДАМАҢЫЗ — ағза зақымдануы және кернеулі пневмоторакс қаупі бар.\n" +
                "Мойын жаралары: абайлап тығыздаңыз; трахеяға (тыныс жолы) қысым жасаудан аулақ болыңыз.",
            stepImage = woundPackingSprite,
            stepPrefab = bleedingWoundPackingPrefab
            // видео нет
        });

        // ★ ВИДЕО — Наложение жгута (точная техника важна)
        steps.Add(new ScenarioStep
        {
            title = "Жгут салу",
            description =
                "Қолданыңыз: қысыммен тоқтатуға болмайтын аяқ-қолдағы артериялық қан кету (ашық қызыл, соғады).\n" +
                "Жараның 5–8 см (2–3 дюйм) жоғарысына қойыңыз (проксимальды, кеудеге қарай).\n" +
                "Қан кету ТОЛЫҚ тоқтағанша және шеткі пульс жоғалғанша тартыңыз.\n" +
                "Салған уақытын нақты белгілеңіз.",
            information =
                "Жгут — бақылаусыз аяқ-қол қан кетуі кезіндегі өмірді сақтайтын соңғы шара. Дұрыс салынған " +
                "жгуттар — 2 сағатқа дейін орнында қалса да — сирек тұрақты зақым келтіреді.\n\n" +
                "ЖГУТ ТҮРЛЕРІ:\n" +
                "• Коммерциялық ұршықты (CAT, SOFTT-W): ең тиімді; артықшылықты.\n" +
                "• Суырылма: кең, серпімді емес таспа (белдік, галстук, жыртылған киім ≥ 4 см ені).\n" +
                "  Шнур, сым немесе арқан ЕШҚАШАН қолданбаңыз — олар қан кетуді тоқтатпай жүйке зақымын тудырады.\n\n" +
                "САЛУ ҚАДАМДАРЫ (коммерциялық ұршықты):\n" +
                "1. Жгутты аяқ-қолмен жараның 5–8 см жоғарысына тартып апарыңыз. Таспаны тоқпен бекітіңіз.\n" +
                "2. Ұршық таяқшасын пайдаланбай тұрып бос ұшты мүмкіндігінше қатты тартыңыз.\n" +
                "3. Барлық қан кету тоқтағанша ұршықты бурап алыңыз. Әдетте 3–5 толық айналым.\n" +
                "4. Ұршықты клипке бекітіңіз. Бекіту таспасын ұршықтың үстінен жабыңыз.\n" +
                "5. Салған уақытын жәбірленушінің маңдайына, білегіне немесе жгуттың өзіне жазыңыз.\n\n" +
                "• Қауіпсіз уақыт: маңызды ишемиялық қауіпке дейін ≤ 2 сағат.\n" +
                "• Далада ешқашан алмаңыз.\n" +
                "• Қан кету қайта басталса: бірінші жгуттан дереу жоғарыға екінші жгут салыңыз.",
            warnings =
                "Буынның үстіне (шынтақ, тізе) ЕШҚАШАН салмаңыз — аяқ-қолдың ортасын қолданыңыз.\n" +
                "Жгутты ЕШҚАШАН жаппаңыз — жедел жәрдем қызметкерлері оны дереу көруі керек.",
            stepImage = tourniquetSprite,
            stepPrefab = bleedingTourniquetPrefab,
            stepVideoClip = bleedingTourniquetVideo,  // ★ ВИДЕО
            quiz = new QuizData
            {
                question = "Жгутты жараның қаншалықты жоғарысына қою керек?",
                answers = new string[]
                {
                "Тікелей жараның үстіне",
                "Жараның 5–8 см жоғарысына",
                "Ең жақын буынға"
                },
                correctAnswerIndex = 1,
                timeLimit = 15f
            }
        });

        steps.Add(new ScenarioStep
        {
            title = "Шокты алдын алу",
            description =
                "Жәбірленушіні арқасымен жайпақ жатқызыңыз.\n" +
                "Жамбас, омыртқа немесе төменгі аяқ сынығы күдіксіз болса, аяқтарын 30–40 см көтеріңіз.\n" +
                "Жабылатын кез келген нәрсемен жауып, суық жерден оқшаулаңыз.\n" +
                "Жәбірленушімен үнемі сөйлесіңіз; тыныс алу мен пульсті әр 2 минут бақылаңыз.",
            information =
                "ГЕМОРРАГИЯЛЫҚ ШОК: II класс шамамен 750–1500 мл қан жоғалтуда (жалпы көлемнің 15–30%) басталады. " +
                "Далада емдеу шокты жоймайды — ол уақыт ұтады.\n\n" +
                "ОРНАЛАСТЫРУ:\n" +
                "• Тренделенбург (аяқтар көтерілген): венозды қанды уақытша орталық айналымға жылжытады.\n" +
                "• Аяқтарды КӨТЕРМЕҢіЗ: жамбас сынығы, төменгі аяқ сынығы, омыртқа жарақаты немесе тыныс алу қиындығы болса.\n\n" +
                "ТЕМПЕРАТУРАНЫ БАСҚАРУ:\n" +
                "Гипотермия коагулопатияны нашарлатады ('өлімге жеткізетін триада'). Жәбірленушіні жерден оқшаулаңыз. " +
                "Термо-жамылғы, пальто немесе қол жетімді кез келген нәрсені қолданыңыз. Басын жауып қойыңыз.\n\n" +
                "АУЫЗША ЕШТЕҢЕ БЕРМЕҢІЗ:\n" +
                "Сұйықтық, тамақ немесе дәрі бермеңіз — шұғыл операция қажет болуы мүмкін.",
            warnings =
                "Тамақ немесе су БЕРМЕҢіЗ — жәбірленушіге дереу операция қажет болуы мүмкін.\n" +
                "Жәбірленушіні бір сәтке де ЖАЛҒЫЗ ҚАЛДЫРМАҢЫЗ.",
            stepImage = shockPreventionSprite,
            stepPrefab = bleedingShockPrefab
            // видео нет
        });
    }

    // =====================================================================
    //  AddUnconsciousSteps() — 1 видео на Recovery Position
    // =====================================================================

    void AddUnconsciousSteps()
    {
        steps.Add(new ScenarioStep
        {
            title = "Реакцияны тексеру",
            description =
                "Абайлап жақындаңыз. Екі иықты да қатты қағып, дауыстаңыз:\n" +
                "'Мені естисіз бе? Көздеріңізді ашыңыз!'\n" +
                "КЕЗ КЕЛГЕН реакцияны іздеңіз: көзін ашу, қозғалыс, дыбыс.\n" +
                "Реакция жоқ → дереу жалғастырыңыз.",
            information =
                "AVPU ШКАЛАСЫ (жылдам сана бағалауы):\n" +
                "• A — Ескерту: көздері ашық, сөйлейді, сұрақтарға жауап береді.\n" +
                "• V — Дауыс: тек ауызша ынталандыруға (айқайыңызға) жауап береді.\n" +
                "• P — Ауырсыну: тек ауырсыну ынталандыруына (төс сүйегін уқалау) жауап береді.\n" +
                "• U — Жауапсыз: ешқандай ынталандыруға жауап жоқ.\n\n" +
                "ТӨС СҮЙЕГІН УҚАЛАУ: Жұдырық түю; буындарды төс сүйегіне қысып, 5 секунд қатты уқалаңыз. " +
                "Оң реакция жәбірленуші тірі және ми бағанасының функциясы бар екенін білдіреді.\n\n" +
                "ОМЫРТҚА САҚТАНДЫРУЛАРЫ:\n" +
                "Жәбірленуші биіктен құлаған, көлік апатына ұшыраған немесе бас/мойын жарақаты болса: " +
                "мойынның қозғалуын азайтыңыз. 2-қадамда бас шалқайту орнына жақты итеру тәсілін қолданыңыз.\n\n" +
                "Бағалаудан 112 шақыруға дейінгі уақыт: ең дұрысы 30 секундтан аз.",
            warnings =
                "Омыртқа жарақаты мүмкін болса басты ШАЙҚАМАҢЫЗ.\n" +
                "Уақытты ЖОҒАЛТПАҢЫЗ — 10 секундта реакция болмаса, жалғастырыңыз.",
            stepImage = responsiveCheckSprite,
            stepPrefab = unconsciousResponsePrefab
        });

        steps.Add(new ScenarioStep
        {
            title = "Тыныс жолын ашу",
            description =
                "Бір қолды маңдайға қойыңыз; екінші қолдың екі саусағымен иекті көтеріңіз.\n" +
                "Бет жоғары қарағанша басты артқа шалқайтыңыз.\n" +
                "Омыртқа жарақаты күдігі болса: ТЕК жақты итеру тәсілін қолданыңыз — бас шалқайтпаңыз.",
            information =
                "Есінен айырылған адамда жақ пен тілді ұстап тұратын бұлшықеттер толығымен босаңсиды. Тіл " +
                "артқы жұтқыншаққа кетіп, тыныс жолын бітейді.\n\n" +
                "БАС ШАЛҚАЙТУ + ИЕКТІ КӨТЕРУ (стандартты тәсіл):\n" +
                "1. Алақаның негізін жәбірленушінің маңдайына қойыңыз.\n" +
                "2. Сұқ және ортаңғы саусақтардың ұштарын иектің сүйекті бөлігіне қойыңыз " +
                "   (жұмсақ тінге ЕМЕС — астын қысу тілді қысады).\n" +
                "3. Маңдайды артқа итеріп, ауыз аздап ашылғанша иекті жоғары көтеріңіз.\n\n" +
                "ЖАҚТЫ ИТЕРУ (омыртқа сақтандыру баламасы):\n" +
                "1. Жәбірленушінің басы жағына тізерлеңіз.\n" +
                "2. Екі алақанның негізін бет сүйектеріне қойыңыз.\n" +
                "3. Сұқ саусақтардың ұштарын жақтың бұрыштарының астына ілдіріңіз.\n" +
                "4. Басты шалқайтпай жақты алға итеріңіз.\n\n" +
                "БӨГДЕ ЗАТ ТЕКСЕРУ:\n" +
                "Тыныс жолын аша сала аузына қараңыз. Құсық, қан немесе қатты зат көрсеңіз, тазалаңыз.",
            warnings =
                "Омыртқа жарақаты күдігі: ТЕК жақты итеру тәсілін қолданыңыз — бас шалқайтпаңыз.\n" +
                "Иектің астындағы жұмсақ тінге ҚЫСПАҢЫЗ.",
            stepImage = checkBreathingSprite,
            stepPrefab = unconsciousAirwayPrefab
            // видео нет
        });

        steps.Add(new ScenarioStep
        {
            title = "Тыныс алуды тексеру",
            description =
                "Тыныс жолы ашық: құлағыңызды аузына жақындатып, кеудеге қараңыз.\n" +
                "КӨРІҢІЗ кеуде көтерілуін. ТЫҢДАҢЫЗ тыныс дыбыстарын. СЕЗІҢІЗ бетіңізде ауа ағынын.\n" +
                "10 секундқа дейін санаңыз.\n" +
                "Қалыпты тыныс (2+ тұрақты тыныс) → Қалпына келтіру позициясы.\n" +
                "Тыныс жоқ немесе тек ентігу → Дереу 112 шалыңыз + ӨЖР бастаңыз.",
            information =
                "ҚАЛЫПТЫ мен ҚАЛЫПТЫ ЕМЕС ТЫНЫС АЛУ:\n\n" +
                "ҚАЛЫПТЫ: Тұрақты, ырғақты кеуде көтерілуі; ересектерде жылдамдығы 12–20/мин.\n\n" +
                "АГОНАЛДЫҚ ЕНТІГУ — жиі қалыпты тыныспен шатастырылады:\n" +
                "Тұрақсыз, сирек (< 6/мин), жиі қатты 'қорылдау' немесе 'ентігу' дыбыстары, кеуде " +
                "аз немесе мүлдем қозғалмайды. Өмірді ұстап тұруға ЖЕТКІЛІКСІЗ. Жүрек тоқтауы ретінде қараңыз.\n\n" +
                "ШЕШІМ АҒАШЫ:\n" +
                "Тыныс қалыпты ма?  ИӘ → Қалпына келтіру позициясы (4-қадам) + бақылау.\n" +
                "                   ЖОҚ / КҮМӘНДІ → 112 шалыңыз + ӨЖР бастаңыз.\n\n" +
                "10 СЕКУНД ЕРЕЖЕСІ:\n" +
                "Тыныс тексеруге 10 секундтан артық уақыт жұмсамаңыз. Күмәнді болса — жүрек тоқтауы ретінде қараңыз.",
            warnings =
                "Агоналдық ентігу — қалыпты тыныс ЕМЕС — ӨЖР дереу бастаңыз.\n" +
                "Күмәнді болса: әрқашан жүрек тоқтауы ретінде қараңыз.",
            stepImage = unconsciousBreathingSprite,
            stepPrefab = unconsciousBreathCheckPrefab
            // видео нет
        });

        // ★ ВИДЕО — Recovery Position (последовательность переворота сложна без визуала)
        steps.Add(new ScenarioStep
        {
            title = "Қалпына келтіру позициясы",
            description =
                "Тек тыныс алу қалыпты және омыртқа жарақаты күдіксіз болса қолданыңыз.\n" +
                "Жәбірленушінің жанына тізерлеңіз. Жақын қолды денеге 90° бұрышта, алақан жоғары қарай жайыңыз.\n" +
                "Алыс қолды кеудеден өткізіп, қолдың сыртын жақын бетке тіреңіз.\n" +
                "Алыс тізені бүгіп, өзіңізге қарай тартып жәбірленушіні бүйіріне аударыңыз.\n" +
                "Басты аздап артқа шалқайтыңыз — ауыз төмен қарауы керек.",
            information =
                "Қалпына келтіру позициясы есінен айырылған, тыныс алып жатқан жәбірленушіні екі өмірге қауіпті " +
                "асқынудан қорғайды: тіл түсуі және құсықты тыныс жолына соруы.\n\n" +
                "ҚАДАМДАР (оң жақ бүйірі):\n" +
                "1. Жәбірленушінің оң жағына тізерлеңіз.\n" +
                "2. Оң қолды денеге 90° бұрышта жайыңыз, шынтақ бүгілген, алақан жоғары.\n" +
                "3. Сол қолды кеудеден өткізіп, сол қолдың сыртын жәбірленушінің оң бетіне тіреңіз.\n" +
                "4. Сол тізеден ұстап, сол аяқ жерге тіреліп тұрғанша жоғары тартыңыз.\n" +
                "5. Тізеден тартып жәбірленушіні өзіңізге қарай аударыңыз. Бет астындағы қолмен жылдамдықты басқарыңыз.\n" +
                "6. Жамбас пен тізе 90° бұрыш жасайтындай үстіңгі аяқты реттеңіз.\n" +
                "7. Басты ақырын артқа шалқайтыңыз — ауыз төмен қарайды.\n\n" +
                "Тыныс алуды әр 2 минут тексеріңіз. Жедел жәрдем кешіксе әр 30 минут орнын ауыстырыңыз.\n" +
                "Тыныс тоқтаса: дереу арқасымен аударып ӨЖР бастаңыз.",
            warnings =
                "Омыртқа жарақаты мүмкін болса (жарақат, биіктен құлау, көлік апаты) ҚОЛДАНБАҢЫЗ.\n" +
                "Үстіңгі аяқ 90° бүгілгеніне көз жеткізіңіз — болмаса жәбірленуші бетімен аударылуы мүмкін.",
            stepImage = recoveryPositionSprite,
            stepPrefab = unconsciousRecoveryPrefab,
            stepVideoClip = unconsciousRecoveryVideo, // ★ ВИДЕО
            quiz = new QuizData
            {
                question = "Қалпына келтіру позициясының негізгі мақсаты не?",
                answers = new string[]
                {
                "ӨЖР-ды оңай бастау үшін",
                "Тіл мен құсықтың тыныс жолын бітемеуі үшін",
                "Пульсті дәлірек тексеру үшін"
                },
                correctAnswerIndex = 1,
                timeLimit = 15f
            }
        });

        steps.Add(new ScenarioStep
        {
            title = "Екінші зерттеу",
            description =
                "Жасырын жарақаттарды тез табу үшін бастан аяқ тексеріңіз.\n" +
                "Қолғаплы қолдармен денені сипаңыз; қан, деформация немесе ісінуді сезіңіз.\n" +
                "Мойын мен білектерде медициналық ескерту зергерлігін тексеріңіз (қант, эпилепсия, аллергия).\n" +
                "Дереу қауіп болмаса жәбірленушіні ҚОЗҒАМАҢЫЗ.",
            information =
                "РЕТТІЛІК (Бастан → Аяқ ұшына):\n" +
                "БАС: Томпақ, шұңқыр, жара немесе қан іздеңіз. Қан немесе мөлдір сұйықтыққа (ЖСС — бас сүйек сынуы мүмкін) " +
                "құлақ пен мұрынды тексеріңіз.\n\n" +
                "БЕТ/МОЙЫН: Қарашықтардың симметриясын тексеріңіз. Мойынды жаралар, трахея ығысуы үшін қараңыз. " +
                "Медициналық ескерту алқалары немесе татуировкалар.\n\n" +
                "КЕУДЕ: Асимметриялық көтерілуді, ашық жараларды, деформацияны тексеріңіз. Екі жақты ақырын қысыңыз — " +
                "ауырсыну немесе тұрақсыздық қабырға сынуын білдіреді.\n\n" +
                "ІШ: Көгеру, кеңею, көрінетін жараларды іздеңіз.\n\n" +
                "ЖАМБАС: Мықын қырқаларына жұмсақ ішкі қысым — ауырсыну немесе қозғалыс жамбас сынуын білдіреді.\n\n" +
                "АЯҚ-ҚОЛ: Деформация, ісіну, бұрылу, ашық сынықтарды тексеріңіз. Білек білезіктерін тексеріңіз.\n\n" +
                "ЖЕДЕЛ ЖӘРДЕМГЕ БЕРЕТІН АҚПАРАТ:\n" +
                "Жығылу уақыты, бұрынғы оқиғалар, жанындағы дәрілер, медициналық ID, тыныс алу өзгерістері.",
            warnings =
                "Омыртқа жарақаты күдігі болса екінші зерттеу үшін жәбірленушіні ҚОЗҒАМАҢЫЗ.\n" +
                "Кірген заттарды АЛМАҢЫЗ.",
            stepImage = secondarySurveySprite,
            stepPrefab = unconsciousSurveyPrefab
            // видео нет
        });

        steps.Add(new ScenarioStep
        {
            title = "Бақылау және қайта бағалау",
            description =
                "Жасалмаса 112 шалыңыз.\n" +
                "Тыныс алуды әр 2 минут тексеріңіз.\n" +
                "Суық жерден оқшаулаңыз; жамылғымен жауып қойыңыз.\n" +
                "Жәбірленушімен тыныш сөйлесіңіз — есінен айырылған науқастар да естуі мүмкін.\n" +
                "Тыныс тоқтаса ӨЖР бастауға дайын болыңыз.",
            information =
                "БАҚЫЛАУ ТІЗІМІ (әр 2 минут):\n" +
                "1. Тыныс алу: Кеуде тұрақты көтеріле ме? Тыныс дыбыстары естіле ме?\n" +
                "2. Түс: Бозғылт, алапат, сұр немесе көк тері = перфузия нашарлауы.\n" +
                "3. Реакция деңгейі: Жақсарту бар ма (ыңырсу, қозғалу, көз ашу)?\n" +
                "4. Тыныс жолы: Позиция сақталуда ма? Құсу болды ма?\n\n" +
                "ЖЕДЕЛ ЖӘРДЕМ КЕЛГЕНДЕ АЙТАТЫН АҚПАРАТ:\n" +
                "• Тапқан уақытыңыз және бастапқы жағдайы.\n" +
                "• Келгендегі AVPU балы және кез келген өзгерістер.\n" +
                "• Кез келген шаралар: ӨЖР, жгут, жараны тығыздау, қалпына келтіру позициясы.\n" +
                "• Табылған медициналық ID немесе дәрілер.\n\n" +
                "ТЫНЫС ТОҚТАСА:\n" +
                "Дереу арқасымен аударыңыз → тыныс жолын қайта ашыңыз → 10 сек тыныс тексеріңіз → ӨЖР бастаңыз.",
            warnings =
                "Кез келген сәтте тыныс тоқтаса — дереу ӨЖР бастаңыз.\n" +
                "Жәбірленушіні жалғыз ҚАЛДЫРМАҢЫЗ.",
            stepImage = monitoringSprite,
            stepPrefab = unconsciousMonitorPrefab
            // видео нет
        });
    }
}
