using GLTFast;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
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

[System.Serializable]
public class ScenarioStep
{
    public string title;

    [TextArea(3, 10)]
    public string description;

    // НОВОЕ — Большой текст с подробной информацией
    [TextArea(5, 15)]
    public string information;

    [TextArea(2, 6)]
    public string warnings;

    public Sprite stepImage;

    public GameObject stepPrefab;

    public string modelUrl;

    public bool enableBodyTracking;

    public QuizData quiz;
}

public class ScenarioController : MonoBehaviour
{
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI stepTitleText;
    public TextMeshProUGUI descriptionText;

    // НОВОЕ — UI для подробной информации
    public TextMeshProUGUI informationText;

    public TextMeshProUGUI warningText;

    public Image stepImageUI;
    public Button nextButton;
    public Button prevButton;
    public GameObject practiceButton;
    public GameObject watchButton;

    // --- UI для Теста ---
    [Header("Quiz UI")]
    public GameObject quizPanel;
    public TextMeshProUGUI quizQuestionText;
    public TextMeshProUGUI timerText;
    public Button[] answerButtons;

    // Prefabs для CPR шагов
    public GameObject cprCheckPrefab;
    public GameObject cprCallPrefab;
    public GameObject cprCompressPrefab;

    // Остальные сценарии
    public GameObject chokingPrefab;
    public GameObject bleedingPrefab;
    public GameObject unconsciousPrefab;

    [Header("AR")]
    public ARRaycastManager raycastManager;

    private static List<ARRaycastHit> hits = new List<ARRaycastHit>();

    private GameObject currentModel;
    private GameObject pendingPrefab;
    private string pendingModelUrl; // Ждет тапа по экрану для скачивания

    [Header("IMGS")]
    public Sprite safetySprite;
    public Sprite checkBreathingSprite;
    public Sprite heimlichSprite;

    [Header("Interaction Settings")]
    public float rotationSpeed = 0.5f;

    private List<ScenarioStep> steps = new List<ScenarioStep>();
    private int currentStepIndex = 0;

    // --- Переменные для таймера ---
    private bool isQuizActive = false;
    private float timeLeft;

    IEnumerator Start()
    {
        Application.targetFrameRate = 30;

        if (quizPanel != null) quizPanel.SetActive(false);

        while (ScenarioManager.Instance == null)
        {
            yield return null;
        }

        yield return null;

        if (ScenarioManager.Instance.isCustomScenario)
        {
            LoadCustomScenario(ScenarioManager.Instance.currentCustomScenario);
        }
        else
        {
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
            ScenarioStep newStep = new ScenarioStep();
            newStep.title = customStep.title;
            newStep.description = customStep.description;
            // Если в CustomScenario нет information, дублируем description или оставляем пустым
            newStep.information = customStep.description;
            newStep.warnings = customStep.warnings;

            // Передаем URL модели из JSON
            newStep.modelUrl = customStep.modelUrl;

            steps.Add(newStep);
        }

        ShowStep();
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
        }

        ShowStep();
    }

    void ShowStep()
    {
        if (steps.Count == 0 || currentStepIndex >= steps.Count)
            return;

        ScenarioStep step = steps[currentStepIndex];

        if (stepTitleText) stepTitleText.text = step.title;
        if (descriptionText) descriptionText.text = step.description;

        // НОВОЕ — Вывод информации на панель
        if (informationText) informationText.text = step.information;

        if (warningText) warningText.text = "<color=red>" + step.warnings + "</color>";

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

        if (currentModel != null)
            Destroy(currentModel);

        pendingPrefab = null;
        pendingModelUrl = null;

        if (!string.IsNullOrEmpty(step.modelUrl))
        {
            pendingModelUrl = step.modelUrl;
        }
        else
        {
            pendingPrefab = step.stepPrefab;
        }

        if (prevButton != null)
        {
            prevButton.interactable = (currentStepIndex > 0);
        }

        // --- Запуск Теста, если он есть в шаге ---
        if (step.quiz != null && !string.IsNullOrEmpty(step.quiz.question))
        {
            StartQuiz(step.quiz);
        }
        else
        {
            if (quizPanel != null) quizPanel.SetActive(false);
            isQuizActive = false;
        }
    }

    void StartQuiz(QuizData data)
    {
        isQuizActive = true;
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
        currentStepIndex = 0;
        ShowStep();
    }

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
        if (isQuizActive) return; // Блокируем кнопку Next, пока не ответит на тест

        currentStepIndex++;

        if (currentStepIndex >= steps.Count)
        {
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

    void AddCPRSteps()
    {
        steps.Add(new ScenarioStep
        {
            title = "Ensure Safety",
            description =
            "Убедитесь, что место происшествия безопасно для вас и пострадавшего.\n" +
            "Проверьте наличие опасностей: ток, движение машин, газ или вода.",
            information =
"Перед тем как приблизиться к пострадавшему, необходимо убедиться, что место происшествия безопасно. Согласно международным рекомендациям по первой помощи, спасатель не должен становиться второй жертвой. Остановитесь на несколько секунд и внимательно осмотрите окружающую обстановку вокруг себя и пострадавшего. Проверьте наличие опасностей: дорожное движение, пожар, дым, утечка газа, открытые электрические провода, вода рядом с источником электричества, обрушение конструкций, стекло, острые предметы, агрессивные животные или люди.\n\nЕсли происшествие произошло на дороге, сначала убедитесь, что транспорт остановлен или находится на безопасном расстоянии. Если рядом есть огонь, запах газа или химические вещества, не приближайтесь к пострадавшему, пока опасность не будет устранена. При подозрении на электротравму никогда не касайтесь человека, пока источник тока не отключён.\n\nПри наличии крови, рвоты или других биологических жидкостей по возможности используйте медицинские перчатки, маску или любой защитный барьер. Если специальных средств нет, можно использовать пакет, ткань или другую защиту для рук.\n\nТолько после того как место происшествия станет безопасным, можно подходить к пострадавшему и начинать оценку его состояния. Если обстановка остаётся опасной, немедленно вызовите экстренные службы и дождитесь профессиональной помощи.",
            warnings =
            "НИКОГДА не становитесь второй жертвой.\nЕсли место опасно — не приближайтесь.",
            stepPrefab = cprCheckPrefab
        });

        steps.Add(new ScenarioStep
        {
            title = "Check Responsiveness",
            description =
            "Громко окликните: 'Вам нужна помощь?'\n" +
            "Аккуратно встряхните за плечи.\n" +
            "Проверьте наличие реакции (стон, открытие глаз, движение).",
            information =
"После того как место происшествия признано безопасным, необходимо быстро проверить, находится ли человек в сознании. Подойдите к пострадавшему со стороны головы или плеч. Это позволит ему увидеть вас, если он откроет глаза, и уменьшит риск случайного движения шеи.\n\nГромко обратитесь к человеку: «Вы меня слышите? Вам нужна помощь? Что случилось?». Одновременно аккуратно положите руки ему на плечи и слегка встряхните. Не трясите голову и не сгибайте шею, так как у пострадавшего может быть травма позвоночника.\n\nОцените любую реакцию. Признаками сознания являются: открывание глаз, движение рук или ног, попытка говорить, стон, кашель, поворот головы, моргание или даже слабое шевеление. Если человек реагирует, оставьте его в удобном положении, выясните, что произошло, и при необходимости вызовите скорую помощь.\n\nЕсли никакой реакции нет, необходимо немедленно считать человека без сознания. Потратьте на проверку не более 5–10 секунд. После этого сразу переходите к оценке дыхания и вызову экстренных служб. Любая задержка уменьшает вероятность выживания при остановке сердца.",
            warnings =
            "НЕ трясите голову — возможна травма шеи.\nПотратьте на это не более 5-10 секунд.",
            stepPrefab = cprCheckPrefab
        });

        steps.Add(new ScenarioStep
        {
            title = "Check Breathing",
            description =
            "Запрокиньте голову назад, поднимите подбородок.\n" +
            "Приложите ухо к губам, глядя на грудную клетку (метод 'Слышу, Вижу, Чувствую').\n" +
            "Ищите нормальные вдохи.",
            information =
"У человека без сознания язык может западать и перекрывать дыхательные пути. Поэтому перед проверкой дыхания необходимо открыть дыхательные пути. Для этого положите одну руку на лоб пострадавшего, а двумя пальцами другой руки аккуратно поднимите подбородок вверх. Голова должна быть слегка запрокинута назад. Этот приём называется «запрокидывание головы и подъём подбородка».\n\nПосле открытия дыхательных путей приблизьте своё ухо ко рту и носу пострадавшего и одновременно смотрите на его грудную клетку. В течение не более 10 секунд используйте правило «Слышу, Вижу, Ощущаю».\n\nСЛЫШУ — прислушайтесь, слышен ли звук вдохов и выдохов.\nВИЖУ — наблюдайте, поднимается и опускается ли грудная клетка.\nОЩУЩАЮ — попытайтесь почувствовать поток воздуха своей щекой.\n\nНормальное дыхание должно быть ровным, регулярным и сопровождаться заметным движением грудной клетки. Редкие, шумные, судорожные вдохи не считаются нормальным дыханием. Такое дыхание называется агональным и часто возникает в первые минуты после остановки сердца. Многие люди ошибочно принимают его за признак жизни и слишком поздно начинают сердечно-лёгочную реанимацию.\n\nЕсли дыхание отсутствует, вы не уверены в его наличии или наблюдаются только редкие судорожные вдохи, необходимо немедленно считать, что дыхания нет, вызвать помощь и начать СЛР.",
            warnings =
            "Агональное дыхание (редкие судорожные вздохи) — это НЕ норма.\nЕсли дыхания нет или оно сомнительно — начинайте СЛР.",
            stepImage = checkBreathingSprite,
            stepPrefab = cprCheckPrefab
        });

        steps.Add(new ScenarioStep
        {
            title = "Call Emergency & Get AED",
            description =
            "Позвоните 112 (или местный номер экстренных служб).\n" +
            "Укажите точный адрес и состояние (без сознания, не дышит).\n" +
            "Громко попросите окружающих принести АНД (AED).",
            information =
"Как только установлено, что человек без сознания и не дышит нормально, необходимо немедленно вызвать экстренные службы. Чем раньше будет вызвана профессиональная помощь, тем выше вероятность выживания. В Казахстане следует звонить по номеру 112 или 103.\n\nЕсли вы один, используйте громкую связь на телефоне, чтобы одновременно разговаривать с диспетчером и начинать реанимацию. Сообщите диспетчеру спокойным и чётким голосом: точный адрес, ориентиры, возраст пострадавшего (если известен), что человек без сознания и не дышит. Не кладите трубку, пока диспетчер не скажет это сделать. Он может давать инструкции по проведению СЛР до прибытия скорой помощи.\n\nЕсли рядом находятся другие люди, обращайтесь не ко всем сразу, а к конкретному человеку. Например: «Вы, в чёрной куртке, позвоните 112». «Вы, принесите автоматический наружный дефибриллятор». Такое обращение значительно повышает вероятность того, что помощь действительно будет оказана.\n\nАвтоматический наружный дефибриллятор (АНД, AED) может находиться в торговых центрах, аэропортах, вокзалах, школах, спортивных залах, офисах и других общественных местах. Чем раньше его удастся использовать, тем больше шанс восстановить нормальный сердечный ритм.",
            warnings =
            "Включите громкую связь на телефоне, чтобы руки были свободны.",
            stepPrefab = cprCallPrefab
        });

        steps.Add(new ScenarioStep
        {
            title = "Chest Compressions",
            description =
            "Основание ладони — на центр груди. Вторая рука сверху в замок.\n" +
            "Глубина: строго 5–6 см.\n" +
            "Темп: 100–120 в минуту (под ритм 'Stayin' Alive').",
            information =
"После 30 компрессий необходимо выполнить 2 искусственных вдоха. Искусственное дыхание помогает доставить кислород в лёгкие пострадавшего. Однако эффективность вдохов зависит от того, открыты ли дыхательные пути. Поэтому сначала снова запрокиньте голову и поднимите подбородок.\n\nЗажмите нос пострадавшего пальцами руки, находящейся на лбу. Сделайте обычный вдох. Затем плотно прижмите свои губы к губам пострадавшего так, чтобы воздух не выходил наружу. Медленно выдыхайте примерно в течение 1 секунды. Следите за грудной клеткой: она должна слегка подняться. Это означает, что воздух попал в лёгкие.\n\nПосле первого вдоха дайте грудной клетке опуститься и повторите второй вдох. Затем сразу же вернитесь к компрессиям. Вся пауза на два вдоха должна занимать не более 10 секунд.\n\nЕсли грудная клетка не поднимается, возможно, дыхательные пути закрыты. В этом случае ещё раз запрокиньте голову, проверьте положение подбородка и попробуйте снова. Не тратьте слишком много времени на попытки.\n\nЕсли у вас нет защитной маски, вы не обучены проведению искусственного дыхания или не готовы делать вдохи, официальные рекомендации допускают проведение только непрерывных компрессий грудной клетки до приезда скорой помощи.",
            warnings =
            "Давайте грудной клетке полностью расправиться после каждого нажатия.\nМинимизируйте паузы между компрессиями.",
            stepImage = safetySprite,
            stepPrefab = cprCompressPrefab,
            enableBodyTracking = true,
            quiz = new QuizData
            {
                question = "Каким должен быть темп нажатий при СЛР?",
                answers = new string[] { "60-80 в минуту", "100-120 в минуту", "Больше 150 в минуту" },
                correctAnswerIndex = 1,
                timeLimit = 15f
            }
        });

        steps.Add(new ScenarioStep
        {
            title = "Rescue Breaths (30:2)",
            description =
            "После 30 нажатий сделайте 2 выдоха 'рот в рот'.\n" +
            "Зажмите нос, плотно обхватите рот своими губами.\n" +
            "Выдох длится 1 секунду до поднятия грудной клетки.",
            information =
"Автоматический наружный дефибриллятор предназначен для распознавания опасных нарушений сердечного ритма и может дать электрический разряд, который восстановит нормальную работу сердца. Современные дефибрилляторы специально разработаны так, чтобы ими могли пользоваться даже люди без медицинского образования.\n\nКак только дефибриллятор оказался рядом, сразу включите его. Большинство устройств начинает автоматически давать голосовые инструкции. Не выключайте прибор и внимательно выполняйте каждое указание.\n\nОголите грудную клетку пострадавшего. Если кожа мокрая, вытрите её. Если грудь покрыта густыми волосами, а в комплекте есть бритва, быстро удалите волосы в местах наклеивания электродов. Это нужно для хорошего контакта.\n\nПервый электрод приклейте под правую ключицу. Второй — на левый бок грудной клетки ниже подмышки. На каждом электроде обычно нарисована схема правильного расположения. После наклеивания электродов дефибриллятор начнёт анализ ритма.\n\nВо время анализа никто не должен касаться пострадавшего. Громко скажите: «Не трогать! Всем отойти!». Если прибор рекомендует разряд, ещё раз убедитесь, что никто не касается человека, и нажмите кнопку разряда, если это требуется. Некоторые устройства делают это автоматически.\n\nСразу после разряда или если разряд не рекомендован, немедленно возобновите компрессии грудной клетки. Не тратьте время на дополнительную проверку пульса или дыхания. Продолжайте СЛР до прибытия скорой помощи или появления признаков жизни.",
            warnings =
            "Если нет защитной маски или вы не умеете — делайте ТОЛЬКО нажатия.\nПауза на вдохи не должна превышать 10 секунд.",
            stepPrefab = cprCompressPrefab
        });

        steps.Add(new ScenarioStep
        {
            title = "Use AED",
            description =
            "Как только принесли АНД — включите его.\n" +
            "Следуйте голосовым инструкциям аппарата.\n" +
            "Приклейте электроды на голую сухую грудь.",
            information =
            "Как только АНД доставлен на место, немедленно включите его и строго следуйте голосовым подсказкам прибора. Оголите грудь пострадавшего. Если она мокрая — протрите её насухо. Если имеется густой волосяной покров — сбрейте его в местах наклеивания электродов. Наклейте электроды точно так, как показано на картинках на самих электродах: один — под правой ключицей, второй — на левый бок чуть ниже подмышки. Прибор начнет анализ сердечного ритма. Громко скомандуйте: «Всем отойти, не касаться пациента!».",
            warnings =
            "НЕ касайтесь пациента во время анализа ритма и разряда.\nПродолжайте СЛР сразу после разряда.",
            stepPrefab = cprCompressPrefab
        });
    }

    void AddChokingSteps()
    {
        steps.Add(new ScenarioStep
        {
            title = "Assess Severity",
            description =
            "Спросите: 'Вы подавились?'\n" +
            "Проверьте, может ли человек говорить, кашлять или дышать.\n" +
            "Ищите характерный жест: руки на горле.",
            information =
            "Срочно оцените ситуацию. Подойдите к человеку и громко спросите: «Вы подавились? Вы можете говорить?». Частичная обструкция: Человек может кашлять, издавать звуки, плакать или говорить с трудом. Кожа сохраняет нормальный цвет. В этом случае просто поощряйте его продолжать активно кашлять. Не хлопайте по спине — это может протолкнуть предмет глубже! Полная обструкция: Человек не может издать ни звука, не может кашлять, хватается руками за горло. Лицо начинает краснеть, а затем синеть. Действовать нужно немедленно.",
            warnings =
            "НЕ мешайте, если человек сильно кашляет.\nПоощряйте кашель, но будьте готовы действовать.",
            stepPrefab = chokingPrefab
        });

        steps.Add(new ScenarioStep
        {
            title = "5 Back Blows",
            description =
            "Наклоните пострадавшего вперед.\n" +
            "Нанесите 5 резких ударов основанием ладони между лопаток.\n" +
            "Цель: выбить инородное тело силой удара.",
            information =
            "Встаньте сбоку и немного сзади от пострадавшего. Одной рукой крепко поддерживайте его грудную клетку, чтобы он не упал вперед. Наклоните пострадавшего сильно вперед (чтобы инородное тело при выталкивании выпало изо рта, а не упало обратно в дыхательные пути). Основанием свободной ладони нанесите до 5 резких, сильных ударов строго между лопатками. Цель каждого удара — создать вибрацию и перепад давления для выбивания предмета. Проверяйте после каждого удара, не вылетело ли инородное тело.",
            warnings =
            "НЕ бейте слишком слабо — удары должны быть ощутимыми.\nПоддерживайте грудь второй рукой.",
            stepPrefab = chokingPrefab
        });

        steps.Add(new ScenarioStep
        {
            title = "Abdominal Thrusts (Heimlich Maneuver)",
            description =
            "Встаньте сзади, обхватите руками талию.\n" +
            "Сложите руку в кулак выше пупка, но ниже ребер.\n" +
            "Сделайте 5 резких толчков 'на себя и вверх'.",
            information =
            "Если 5 ударов по спине не помогли, переходите к абдоминальным толчкам. Встаньте позади пострадавшего и обхватите его талию обеими руками. Сожмите одну кисть в кулак и приложите его большим пальцем к животу пострадавшего — строго посередине между пупком и мечевидным отростком. Обхватите свой кулак второй рукой. Сделайте резкий, сильный толчок направленный ВНУТРЬ и ВВЕРХ (в форме буквы J). Повторите до 5 раз. Чередуйте: 5 ударов по спине — 5 толчков в живот, пока предмет не выйдет наружу.",
            warnings =
            "НЕ давите на мечевидный отросток или ребра.\nДля беременных или тучных людей делайте толчки В ГРУДЬ.",
            stepPrefab = chokingPrefab
        });

        steps.Add(new ScenarioStep
        {
            title = "If Victim Collapses",
            description =
            "Если человек потерял сознание, аккуратно опустите его на пол.\n" +
            "Немедленно вызывайте 112.\n" +
            "Начинайте стандартную СЛР (30 нажатий на грудь).",
            information =
            "Из-за длительной нехватки кислорода человек потеряет сознание. Ваша задача — аккуратно подхватить его, чтобы предотвратить травму головы при падении, и положить на ровную твердую поверхность (пол). Немедленно вызовите экстренные службы (112), если это еще не сделано. Сразу же приступайте к Сердечно-Легочной Реанимации (СЛР), начиная с 30 компрессий грудной клетки. Компрессии могут создать достаточное давление в грудной клетке, чтобы вытолкнуть предмет. Каждый раз перед искусственным вдохом широко открывайте рот пострадавшему и осматривайте его.",
            warnings =
            "НЕ пытайтесь достать предмет пальцем вслепую.\nПроверяйте рот на наличие предмета только ПЕРЕД вдохом.",
            stepPrefab = chokingPrefab
        });
    }

    void AddBleedingSteps()
    {
        steps.Add(new ScenarioStep
        {
            title = "Direct Pressure",
            description =
            "Наложите чистую повязку или ткань прямо на рану.\n" +
            "Давите максимально сильно обеими руками.\n" +
            "Если кровь пропитывает ткань, наложите вторую сверху, не снимая первую.",
            information =
            "Критическое (массивное) кровотечение убьет человека за 2-3 минуты. Действовать нужно мгновенно. Возьмите самый чистый материал, который есть под рукой (марлевая салфетка, бинт, футболка, полотенце). Наложите его прямо на источник кровотечения в ране и надавите с максимальным усилием обеими руками. Если позволяет ситуация, используйте вес своего тела для создания давления. Если повязка быстро пропиталась кровью, НИКОГДА не снимайте её! Вы сорвете формирующийся кровяной сгусток. Просто положите сверху новые слои ткани и давите еще сильнее.",
            warnings =
            "НЕ снимайте пропитавшуюся ткань — вы разрушите формирующийся тромб.\nНЕ вынимайте глубоко засевшие предметы (нож, стекло).",
            stepPrefab = bleedingPrefab
        });

        steps.Add(new ScenarioStep
        {
            title = "Wound Packing",
            description =
            "Если рана глубокая (в паху, подмышке или на шее), плотно заполните её тканью или бинтом.\n" +
            "Продолжайте давить сверху всем весом тела.",
            information =
            "Если рана глубокая и находится в так называемых «узловых зонах» (шея, подмышечные впадины, паховая область), где наложить жгут невозможно, необходимо выполнить тампонаду. Найдите источник кровотечения (поврежденный сосуд) внутри раны. Используя гемостатический бинт (или обычный чистый бинт), плотно, палец за пальцем, утрамбовывайте ткань прямо внутрь раневого канала до самого дна, пока рана не заполнится полностью. После того как рана туго набита бинтом, продолжайте оказывать сильнейшее прямое давление сверху обеими руками в течение минимум 3-5 минут.",
            warnings =
            "НЕ делайте тампонаду в области грудной клетки или живота (риск повреждения внутренних органов).",
            stepPrefab = bleedingPrefab
        });

        steps.Add(new ScenarioStep
        {
            title = "Apply Tourniquet",
            description =
            "Если кровь бьет фонтаном и давление не помогает, наложите жгут выше раны (на 5-8 см).\n" +
            "Затягивайте до полной остановки кровотечения.\n" +
            "Запишите время наложения на видном месте.",
            information =
            "Если прямое давление неэффективно, или кровотечение носит массивный артериальный характер (алая кровь бьет пульсирующим фонтаном) на руке или ноге — немедленно используйте жгут. Наложите жгут на 5-8 см выше раны (ближе к туловищу). Затягивайте жгут (или вращайте вороток турникета) до тех пор, пока кровотечение полностью не остановится и не исчезнет пульс ниже жгута. Надежно зафиксируйте жгут. Обязательно зафиксируйте точное время наложения (маркером на лбу пострадавшего, на самом турникете или в телефоне).",
            warnings =
            "НЕ накладывайте жгут на суставы (локоть, колено).\nЖгут — это больно, предупредите об этом пострадавшего.",
            stepPrefab = bleedingPrefab
        });

        steps.Add(new ScenarioStep
        {
            title = "Shock Prevention",
            description =
            "Уложите пострадавшего на спину.\n" +
            "Укройте его, чтобы сохранить тепло.\n" +
            "Постоянно разговаривайте, проверяя уровень сознания.",
            information =
            "Потеря большого объема крови неминуемо ведет к развитию шока. Уложите пострадавшего на спину. Если нет подозрений на травмы ног или таза, приподнимите его ноги на высоту 30-40 см — это обеспечит приток оставшейся крови к жизненно важным органам. Пострадавший с кровопотерей быстро теряет тепло. Обязательно укройте его термоодеялом, пледом или куртками. Изолируйте его от холодной земли. Постоянно находитесь рядом, контролируйте пульс и уровень сознания, успокаивайте человека.",
            warnings =
            "НЕ давайте пить или есть (возможна экстренная операция).\nНЕ оставляйте пострадавшего одного.",
            stepPrefab = bleedingPrefab
        });
    }

    void AddUnconsciousSteps()
    {
        steps.Add(new ScenarioStep
        {
            title = "Check Breathing",
            description =
            "Запрокиньте голову пострадавшего назад.\n" +
            "Приложите ухо к губам, глядя на грудную клетку.\n" +
            "Считайте до 10. Вы должны услышать минимум 2-3 нормальных вдоха.",
            information =
            "Убедившись в безопасности и проверив реакцию (сознание отсутствует), необходимо немедленно проверить проходимость дыхательных путей и наличие адекватного дыхания. Запрокиньте голову назад и поднимите подбородок для открытия путей (прием Сафара). Наклонитесь ухом ко рту пациента и считайте до 10. За эти 10 секунд вы должны услышать, увидеть и почувствовать минимум 2-3 нормальных, спокойных вдоха. Если человек находится без сознания, но дышит нормально, его жизни в данный момент ничто не угрожает.",
            warnings =
            "Агональные вздохи (редкие всхлипы) — это ПРИЗНАК ОСТАНОВКИ сердца.\nЕсли дыхание ненормальное — переходите к алгоритму СЛР (CPR).",
            stepPrefab = unconsciousPrefab
        });

        steps.Add(new ScenarioStep
        {
            title = "Secondary Survey",
            description =
            "Быстро осмотрите тело на наличие сильных кровотечений или деформаций конечностей.\n" +
            "Проверьте наличие медицинских браслетов или жетонов.",
            information =
            "Пока ожидаете бригаду скорой помощи, проведите быстрый, но внимательный визуальный и тактильный осмотр тела с головы до ног. Ваша цель — найти скрытые угрозы, в первую очередь: массивные кровотечения (ощупайте одежду в местах, скрытых от глаз, проверьте, нет ли под человеком лужи крови). Обратите внимание на явные деформации конечностей, ожоги, признаки укусов ядовитых насекомых или змей. Осмотрите шею и запястья на наличие медицинских браслетов или кулонов, которые могут подсказать причину комы.",
            warnings =
            "НЕ перемещайте человека, если подозреваете травму позвоночника (падение с высоты, ДТП),\nкроме случаев, когда его жизни угрожает внешняя опасность.",
            stepPrefab = unconsciousPrefab
        });

        steps.Add(new ScenarioStep
        {
            title = "Recovery Position",
            description =
            "Ближнюю к вам руку отведите в сторону под прямым углом.\n" +
            "Дальнюю руку приложите тыльной стороной к противоположной щеке.\n" +
            "Согните дальнюю ногу в колене и поверните человека на бок к себе.",
            information =
            "Если пострадавший без сознания, дышит стабильно, и вы уверены, что у него нет травмы позвоночника, его необходимо перевести в устойчивое боковое (восстановительное) положение. Это защитит дыхательные пути от западания языка и вдыхания рвотных масс. 1. Ближнюю к вам руку согните под прямым углом вверх. 2. Дальнюю руку перекиньте через грудь и прижмите тыльной стороной ладони к ближней к вам щеке пострадавшего. 3. Согните дальнюю от вас ногу в колене под прямым углом. 4. Потяните за согнутое колено на себя, аккуратно переворачивая человека на бок. Верхняя нога должна упираться в землю.",
            warnings =
            "Убедитесь, что голова запрокинута назад, а рот направлен вниз для выхода жидкостей.\nСледите, чтобы верхнее колено подпирало тело, не давая ему скатиться на живот.",
            stepPrefab = unconsciousPrefab
        });

        steps.Add(new ScenarioStep
        {
            title = "Monitor & Reassess",
            description =
            "Вызовите 112, если это не было сделано.\n" +
            "Каждые 2 минуты перепроверяйте наличие нормального дыхания.\n" +
            "Укройте человека, чтобы избежать переохлаждения.",
            information =
            "Даже находясь в восстановительном положении, состояние человека может резко ухудшиться. Вам необходимо непрерывно контролировать его дыхание (каждые 1-2 минуты подносите руку ко рту или наблюдайте за движениями грудной клетки). Защитите человека от переохлаждения или перегрева в зависимости от погоды (используйте термоодеяло). Подготовьтесь передать информацию медикам: во сколько человек потерял сознание, как долго находится в таком состоянии, какие травмы были обнаружены при осмотре, и менялся ли характер дыхания.",
            warnings =
            "Если дыхание прекратилось — немедленно переверните на спину и начинайте СЛР.\nНЕ оставляйте человека без присмотра.",
            stepPrefab = unconsciousPrefab
        });
    }
}