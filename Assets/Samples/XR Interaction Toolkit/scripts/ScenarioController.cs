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
    public float typingSpeed = 0.02f;
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

        // --- AR Модельдер логикасы ---
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
            if (quizPanel != null) quizPanel.SetActive(false);
            isQuizActive = false;
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
            title = "Қауіпсіздікті қамтамасыз ету",
            description =
            "Оқиға орны сіз үшін және зардап шегуші үшін қауіпсіз екеніне көз жеткізіңіз.\n" +
            "Қауіптердің бар-жоғын тексеріңіз: электр тогы, көлік қозғалысы, газ немесе су.",
            information =
"Зардап шегушіге жақындамас бұрын, оқиға орнының қауіпсіз екеніне көз жеткізу керек. Алғашқы көмек көрсетудің халықаралық ұсынымдарына сәйкес, құтқарушы екінші құрбанға айналмауы тиіс. Бірнеше секундқа тоқтап, өзіңіздің және зардап шегушінің айналасын мұқият тексеріңіз. Қауіпті факторларды тексеріңіз: жол қозғалысы, өрт, түтін, газдың шығуы, ашық электр сымдары, электр көзінің жанындағы су, конструкциялардың құлауы, шыны, өткір заттар, агрессивті жануарлар немесе адамдар.\n\nЕгер оқиға жолда болса, алдымен көліктің тоқтағанына немесе қауіпсіз қашықтықта екеніне көз жеткізіңіз. Егер жақын жерде өрт, газ иісі немесе химиялық заттар болса, қауіп жойылмайынша зардап шегушіге жақындамаңыз. Электр жарақаты күдігі болса, ток көзі өшірілмейінше адамға ешқашан қол тигізбеңіз.\n\nҚан, құсық немесе басқа биологиялық сұйықтықтар болған жағдайда, мүмкіндігінше медициналық қолғап, маска немесе кез келген қорғаныс кедергісін қолданыңыз. Арнайы құралдар болмаса, пакетті, матаны немесе қолға арналған басқа қорғанысты пайдалануға болады.\n\nОқиға орны қауіпсіз болғаннан кейін ғана зардап шегушіге жақындап, оның жағдайын бағалауды бастауға болады. Егер жағдай қауіпті болып қалса, дереу шұғыл қызметтерді шақырыңыз және кәсіби көмекті күтіңіз.",
            warnings =
            "ЕШҚАШАН екінші құрбан болмаңыз.\nЕгер орын қауіпті болса — жақындамаңыз.",
            stepPrefab = cprCheckPrefab
        });

        steps.Add(new ScenarioStep
        {
            title = "Реакцияны тексеру",
            description =
            "Дауыстап: 'Сізге көмек керек пе?' — деп сұраңыз.\n" +
            "Иығынан ақырын сілкіңіз.\n" +
            "Реакцияның бар-жоғын тексеріңіз (ыңырсу, көз ашу, қозғалыс).",
            information =
"Оқиға орны қауіпсіз деп танылғаннан кейін, адамның есін білетінін тез арада тексеру қажет. Зардап шегушіге бас жағынан немесе иық тұсынан жақындаңыз. Бұл ол көзін ашқан жағдайда сізді көруіне мүмкіндік береді және мойынның кездейсоқ қозғалу қаупін азайтады.\n\nАдамға дауыстап тіл қатыңыз: «Мені естисіз бе? Сізге көмек керек пе? Не болды?». Сонымен қатар, қолыңызды оның иығына ақырын қойып, сәл сілкіңіз. Басын шайқамаңыз және мойнын бүкпеңіз, себебі зардап шегушінің омыртқасы зақымдалуы мүмкін.\n\nКез келген реакцияны бағалаңыз. Ес-түс белгілеріне мыналар жатады: көзді ашу, қолды немесе аяқты қозғалту, сөйлеуге тырысу, ыңырсу, жөтелу, басты бұру, көз қағу немесе тіпті сәл қозғалу. Егер адам жауап берсе, оны ыңғайлы қалыпта қалдырып, не болғанын анықтаңыз және қажет болса, жедел жәрдем шақырыңыз.\n\nЕгер ешқандай реакция болмаса, адамды дереу ес-түссіз деп есептеу керек. Тексеруге 5–10 секундтан артық уақыт жұмсамаңыз. Содан кейін бірден тыныс алуды бағалауға және шұғыл қызметтерді шақыруға көшіңіз. Кез келген кідіріс жүрек тоқтаған кезде аман қалу мүмкіндігін азайтады.",
            warnings =
            "Басын ШАЙҚАМАҢЫЗ — мойын жарақаты болуы мүмкін.\nБұған 5-10 секундтан артық уақыт жұмсамаңыз.",
            stepPrefab = cprCheckPrefab
        });

        steps.Add(new ScenarioStep
        {
            title = "Тыныс алуды тексеру",
            description =
            "Басын артқа шалқайтып, иегін көтеріңіз.\n" +
            "Құлағыңызды ерніне жақындатып, кеуде қуысына қараңыз ('Естимін, Көремін, Сеземін' әдісі).\n" +
            "Қалыпты тыныс алуды іздеңіз.",
            information =
"Ес-түссіз жатқан адамның тілі артқа кетіп, тыныс алу жолдарын жауып қалуы мүмкін. Сондықтан тыныс алуды тексермес бұрын тыныс алу жолдарын ашу керек. Ол үшін бір қолыңызды зардап шегушінің маңдайына қойып, екінші қолыңыздың екі саусағымен иегін ақырын жоғары көтеріңіз. Басы сәл артқа шалқаюы керек. Бұл әдіс «басты шалқайту және иекті көтеру» деп аталады.\n\nТыныс алу жолдарын ашқаннан кейін құлағыңызды зардап шегушінің аузы мен мұрнына жақындатып, сонымен бірге оның кеуде қуысына қараңыз. 10 секундтан асырмай «Естимін, Көремін, Сеземін» ережесін қолданыңыз.\n\nЕСТИМІН — дем алу мен дем шығару дыбысы естіле ме, тыңдаңыз.\nКӨРЕМІН — кеуде қуысының көтеріліп-түскенін бақылаңыз.\nСЕЗЕМІН — ауа ағынын бетіңізбен сезінуге тырысыңыз.\n\nҚалыпты тыныс алу бірқалыпты, жүйелі болуы және кеуде қуысының айқын қозғалысымен бірге жүруі керек. Сирек, шулы, құрысулы тыныс алу қалыпты болып саналмайды. Мұндай тыныс алу агониялық деп аталады және көбінесе жүрек тоқтағаннан кейінгі алғашқы минуттарда пайда болады. Көптеген адамдар оны тіршілік белгісі деп қателесіп, өкпе-жүрек реанимациясын тым кеш бастайды.\n\nЕгер тыныс алу болмаса, оның бар екеніне сенімді болмасаңыз немесе тек сирек құрысулы тыныс байқалса, дереу тыныс жоқ деп есептеп, көмек шақырып, ӨЖР бастау керек.",
            warnings =
            "Агониялық тыныс алу (сирек құрысулы тыныс) — бұл қалыпты ЕМЕС.\nЕгер тыныс болмаса немесе күмәнді болса — ӨЖР бастаңыз.",
            stepImage = checkBreathingSprite,
            stepPrefab = cprCheckPrefab
        });

        steps.Add(new ScenarioStep
        {
            title = "Көмек шақыру және АНД алу",
            description =
            "112 (немесе жергілікті шұғыл қызмет нөміріне) қоңырау шалыңыз.\n" +
            "Нақты мекенжайды және жағдайды (ес-түссіз, дем алмайды) айтыңыз.\n" +
            "Айналадағылардан АНД (AED) аппаратын әкелуді дауыстап сұраңыз.",
            information =
"Адамның ес-түссіз екені және қалыпты тыныс алмайтыны анықталған бойда, дереу шұғыл қызметтерді шақыру қажет. Кәсіби көмек неғұрлым ерте шақырылса, аман қалу ықтималдығы соғұрлым жоғары болады. Қазақстанда 112 немесе 103 нөміріне қоңырау шалу керек.\n\nЕгер сіз жалғыз болсаңыз, диспетчермен сөйлесу және реанимацияны бастау үшін телефонның дауыс зорайтқышын (спикер) пайдаланыңыз. Диспетчерге сабырлы және анық дауыспен хабарлаңыз: нақты мекенжай, бағдарлар, зардап шегушінің жасы (белгілі болса), адамның ес-түссіз екені және дем алмайтыны. Диспетчер айтқанша тұтқаны қоймаңыз. Ол жедел жәрдем келгенге дейін ӨЖР жүргізу бойынша нұсқаулар бере алады.\n\nЕгер қасыңызда басқа адамдар болса, бәріне бірдей емес, нақты бір адамға жүгініңіз. Мысалы: «Сіз, қара күртедегі адам, 112-ге хабарласыңыз». «Сіз, автоматты сыртқы дефибрилляторды әкеліңіз». Мұндай өтініш көмектің шынымен көрсетілу ықтималдығын айтарлықтай арттырады.\n\nАвтоматты сыртқы дефибриллятор (АНД, AED) сауда орталықтарында, әуежайларда, вокзалдарда, мектептерде, спорт залдарында, кеңселерде және басқа да қоғамдық орындарда болуы мүмкін. Оны неғұрлым ерте қолдану мүмкін болса, қалыпты жүрек ырғағын қалпына келтіру мүмкіндігі соғұрлым жоғары болады.",
            warnings =
            "Қолыңыз бос болуы үшін телефонның дауыс зорайтқышын қосыңыз.",
            stepPrefab = cprCallPrefab
        });

        steps.Add(new ScenarioStep
        {
            title = "Кеуде қуысын қысу",
            description =
            "Алақанның негізін кеуденің ортасына қойыңыз. Екінші қолды үстіне қойып, саусақтарды айқастырыңыз.\n" +
            "Тереңдігі: қатаң түрде 5–6 см.\n" +
            "Қарқыны: минутына 100–120 рет",
            information =
"30 компрессиядан кейін 2 рет жасанды тыныс алу керек. Жасанды тыныс алу зардап шегушінің өкпесіне оттегін жеткізуге көмектеседі. Алайда, тыныс алудың тиімділігі тыныс алу жолдарының ашық болуына байланысты. Сондықтан алдымен басын қайтадан шалқайтып, иегін көтеріңіз.\n\nЗардап шегушінің маңдайындағы қолыңыздың саусақтарымен мұрнын қысыңыз. Қалыпты дем алыңыз. Содан кейін ауа сыртқа шықпайтындай етіп ерніңізді зардап шегушінің ерніне тығыз басыңыз. Шамамен 1 секунд ішінде баяу дем шығарыңыз. Кеуде қуысын бақылаңыз: ол сәл көтерілуі керек. Бұл ауаның өкпеге түскенін білдіреді.\n\nБірінші демнен кейін кеуде қуысының төмен түсуін күтіп, екінші демді қайталаңыз. Содан кейін бірден компрессияға оралыңыз. Екі дем алуға арналған үзіліс 10 секундтан аспауы керек.\n\nЕгер кеуде қуысы көтерілмесе, тыныс алу жолдары жабық болуы мүмкін. Бұл жағдайда басын тағы бір рет шалқайтып, иектің қалпын тексеріп, қайтадан көріңіз. Әрекеттерге тым көп уақыт жұмсамаңыз.\n\nЕгер сізде қорғаныс маскасы болмаса, жасанды тыныс алуды жүргізуге үйретілмеген болсаңыз немесе дем салуға дайын болмасаңыз, ресми ұсынымдар жедел жәрдем келгенше тек кеуде қуысын үздіксіз қысуды жүргізуге рұқсат береді.",
            warnings =
            "Әрбір басудан кейін кеуде қуысының толық жазылуына мүмкіндік беріңіз.\nКомпрессиялар арасындағы үзілістерді азайтыңыз.",
            stepImage = safetySprite,
            stepPrefab = cprCompressPrefab,
            enableBodyTracking = true,
            quiz = new QuizData
            {
                question = "ӨЖР кезінде басу қарқыны қандай болуы керек?",
                answers = new string[] { "минутына 60-80", "минутына 100-120", "минутына 150-ден көп" },
                correctAnswerIndex = 1,
                timeLimit = 15f
            }
        });

        steps.Add(new ScenarioStep
        {
            title = "Жасанды тыныс алу (30:2)",
            description =
            "30 рет басудан кейін 2 рет 'ауыздан ауызға' дем салыңыз.\n" +
            "Мұрнын қысып, аузын ерніңізбен тығыз жабыңыз.\n" +
            "Дем шығару кеуде көтерілгенше 1 секундқа созылады.",
            information =
"Автоматты сыртқы дефибриллятор жүрек ырғағының қауіпті бұзылуларын анықтауға арналған және жүректің қалыпты жұмысын қалпына келтіретін электр зарядын бере алады. Қазіргі заманғы дефибрилляторлар арнайы медициналық білімі жоқ адамдар да қолдана алатындай етіп жасалған.\n\nДефибриллятор жаныңызға келген бойда оны бірден қосыңыз. Құрылғылардың көпшілігі автоматты түрде дауыстық нұсқаулар бере бастайды. Құрылғыны өшірмеңіз және әрбір нұсқауды мұқият орындаңыз.\n\nЗардап шегушінің кеуде қуысын ашыңыз. Егер терісі су болса, оны сүртіңіз. Егер кеудеде қалың түк болса және жиынтықта ұстара болса, электродтарды жапсыратын жерлердегі түкті тез арада алып тастаңыз. Бұл жақсы байланыс үшін қажет.\n\nБірінші электродты оң жақ бұғана астына жапсырыңыз. Екіншісін — кеуде қуысының сол жақ бүйіріне, қолтық астынан төменірек қойыңыз. Әр электродта әдетте дұрыс орналасу схемасы салынған. Электродтарды жапсырғаннан кейін дефибриллятор ырғақты талдауды бастайды.\n\nТалдау кезінде ешкім зардап шегушіге қол тигізбеуі керек. Дауыстап: «Тиіспеңіздер! Бәріңіз алыстаңыздар!» — деп айтыңыз. Егер құрылғы зарядты ұсынса, адамға ешкім тиіп тұрмағанына тағы бір рет көз жеткізіп, қажет болса зарядтау түймесін басыңыз. Кейбір құрылғылар мұны автоматты түрде жасайды.\n\nЗарядтан кейін немесе заряд ұсынылмаса, дереу кеуде қуысының компрессиясын қайта бастаңыз. Тамыр соғуын немесе тыныс алуды қосымша тексеруге уақыт жұмсамаңыз. ӨЖР-ды жедел жәрдем келгенше немесе тіршілік белгілері пайда болғанша жалғастырыңыз.",
            warnings =
            "Егер қорғаныс маскасы болмаса немесе білмесеңіз — ТЕК басуды орындаңыз.\nДем алуға арналған үзіліс 10 секундтан аспауы тиіс.",
            stepPrefab = cprCompressPrefab
        });

        steps.Add(new ScenarioStep
        {
            title = "АНД қолдану",
            description =
            "АНД әкелінген бойда — оны қосыңыз.\n" +
            "Аппараттың дауыстық нұсқауларын орындаңыз.\n" +
            "Электродтарды жалаңаш, құрғақ кеудеге жапсырыңыз.",
            information =
            "АНД оқиға орнына жеткізілген бойда, оны дереу қосыңыз және құрылғының дауыстық нұсқауларын қатаң орындаңыз. Зардап шегушінің кеудесін ашыңыз. Егер ол су болса — құрғатып сүртіңіз. Егер қалың түк болса — электродтарды жапсыратын жерлерді қырыңыз. Электродтарды дәл электродтардың өзіндегі суреттерде көрсетілгендей жапсырыңыз: біреуі — оң жақ бұғана астына, екіншісі — сол жақ бүйірге, қолтық астынан сәл төмен. Құрылғы жүрек ырғағын талдауды бастайды. Дауыстап: «Бәріңіз алыстаңыздар, пациентке тиіспеңіздер!» — деп бұйрық беріңіз.",
            warnings =
            "Ырғақты талдау және заряд кезінде пациентке ТИІСПЕҢІЗ.\nЗарядтан кейін бірден ӨЖР-ды жалғастырыңыз.",
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