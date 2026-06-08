using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// ConstructorScreenBuilder — программно создаёт красивый UI для конструктора сценариев.
/// Повесьте на пустой GameObject в сцене scenarioConstructor.
/// Заменяет старый ConstructorController — вся логика встроена.
/// </summary>
public class ConstructorScreenBuilder : MonoBehaviour
{
    [Header("Существующий Canvas (если пусто — создаст новый)")]
    public Canvas existingCanvas;

    [Header("Старый UI (скроется)")]
    public GameObject existingUI;

    [Header("Шрифт (опционально)")]
    public TMP_FontAsset customFont;

    // ── Цвета ────────────────────────────────────────────────────────────────
    private readonly Color bgColor       = new Color(0.07f, 0.08f, 0.12f, 1f);
    private readonly Color headerColor   = new Color(0.12f, 0.14f, 0.22f, 1f);
    private readonly Color cardColor     = new Color(0.11f, 0.13f, 0.18f, 1f);
    private readonly Color inputBgColor  = new Color(0.16f, 0.18f, 0.24f, 1f);
    private readonly Color accentColor   = new Color(0.25f, 0.60f, 1.0f, 1f);
    private readonly Color greenColor    = new Color(0.18f, 0.78f, 0.45f, 1f);
    private readonly Color orangeColor   = new Color(0.95f, 0.60f, 0.15f, 1f);
    private readonly Color redColor      = new Color(0.85f, 0.25f, 0.25f, 1f);
    private readonly Color textColor     = new Color(0.93f, 0.93f, 0.95f, 1f);
    private readonly Color subtextColor  = new Color(0.50f, 0.52f, 0.58f, 1f);
    private readonly Color placeholderCol= new Color(0.40f, 0.42f, 0.50f, 1f);

    // ── Ссылки на UI ─────────────────────────────────────────────────────────
    private Canvas            mainCanvas;
    private TMP_InputField    scenarioNameInput;
    private TMP_InputField    stepTitleInput;
    private TMP_InputField    descriptionInput;
    private TMP_InputField    warningsInput;
    private TMP_InputField    quizInput;
    private TMP_InputField    modelUrlInput;
    private TMP_InputField    videoUrlInput;
    private TextMeshProUGUI   stepCounterText;
    private TextMeshProUGUI   statusText;
    private Button            addStepBtn;
    private Button            saveBtn;

    // ═════════════════════════════════════════════════════════════════════════
    void Start()
    {
        BuildUI();
    }

    // ═════════════════════════════════════════════════════════════════════════
    //  CANVAS
    // ═════════════════════════════════════════════════════════════════════════
    void BuildUI()
    {
        if (existingUI != null) existingUI.SetActive(false);

        if (existingCanvas != null)
        {
            mainCanvas = existingCanvas;
        }
        else
        {
            var go = new GameObject("ConstructorCanvas");
            go.transform.SetParent(transform);
            mainCanvas = go.AddComponent<Canvas>();
            mainCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            mainCanvas.sortingOrder = 100;
            go.AddComponent<GraphicRaycaster>();
        }

        // Принудительно настраиваем CanvasScaler под 1080x2400
        var scaler = mainCanvas.GetComponent<CanvasScaler>();
        if (scaler == null) scaler = mainCanvas.gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 2400);
        scaler.matchWidthOrHeight = 0.5f;

        BuildScreen();
    }

    // ═════════════════════════════════════════════════════════════════════════
    //  ЭКРАН
    // ═════════════════════════════════════════════════════════════════════════
    void BuildScreen()
    {
        var root = MakePanel("Root", mainCanvas.transform, bgColor);

        // ── ХЕДЕР ────────────────────────────────────────────────────────────
        var header = MakePanel("Header", root.transform, headerColor);
        SetRect(header, V2(0,1), V2(1,1), V2(0,-220), V2(0,0));

        // Кнопка назад
        var backGO = MakeButtonObj("BackBtn", header.transform, "←", 52, redColor);
        SetRect(backGO, V2(0,0.5f), V2(0,0.5f), V2(24,-45), V2(130,45));
        backGO.GetComponent<Button>().onClick.AddListener(() => SceneManager.LoadScene("Untitled"));

        // Заголовок
        var titleTMP = MakeText("Title", header.transform, "Сценарий Конструкторы", 52, textColor, TextAlignmentOptions.Center);
        SetRect(titleTMP.gameObject, V2(0.14f,0.3f), V2(0.86f,1), V2(0,0), V2(0,0));

        // Счётчик шагов
        stepCounterText = MakeText("StepCounter", header.transform, "Қадамдар: 0", 36, accentColor, TextAlignmentOptions.Center);
        SetRect(stepCounterText.gameObject, V2(0.14f,0), V2(0.86f,0.35f), V2(0,8), V2(0,0));

        // ── СКРОЛЛ ───────────────────────────────────────────────────────────
        var scrollGO = new GameObject("Scroll");
        scrollGO.transform.SetParent(root.transform, false);
        var scrollRT = scrollGO.AddComponent<RectTransform>();
        SetAnchors(scrollRT, V2(0,0), V2(1,1), V2(0,0), V2(0,-230));
        var scrollRect = scrollGO.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollGO.AddComponent<RectMask2D>();

        var content = new GameObject("Content");
        content.transform.SetParent(scrollGO.transform, false);
        var contentRT = content.AddComponent<RectTransform>();
        contentRT.anchorMin = V2(0,1);
        contentRT.anchorMax = V2(1,1);
        contentRT.pivot = V2(0.5f,1);
        contentRT.offsetMin = contentRT.offsetMax = Vector2.zero;

        var vlg = content.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 24;
        vlg.padding = new RectOffset(40, 40, 30, 160);
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;

        var csf = content.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        scrollRect.content = contentRT;

        // ═════════════════════════════════════════════════════════════════════
        //  КАРТОЧКИ С ПОЛЯМИ
        // ═════════════════════════════════════════════════════════════════════

        // ── Название сценария ────────────────────────────────────────────────
        var nameCard = MakeCard(content.transform, "Сценарий атауы", 170);
        scenarioNameInput = MakeInputField(nameCard.transform, "Мысалы: Жүрек ұстамасы", 100, false);

        // ── Шаг: Заголовок ───────────────────────────────────────────────────
        var titleCard = MakeCard(content.transform, "Қадам тақырыбы *", 170);
        stepTitleInput = MakeInputField(titleCard.transform, "Мысалы: Қауіпсіздікті тексеру", 100, false);

        // ── Описание ─────────────────────────────────────────────────────────
        var descCard = MakeCard(content.transform, "Қадам сипаттамасы *", 320);
        descriptionInput = MakeInputField(descCard.transform, "Не істеу керек екенін жазыңыз...", 250, true);

        // ── Предупреждения ───────────────────────────────────────────────────
        var warnCard = MakeCard(content.transform, "Ескертулер *", 260);
        warningsInput = MakeInputField(warnCard.transform, "Не істеуге болмайтынын жазыңыз...", 190, true);

        // ── Квиз ─────────────────────────────────────────────────────────────
        var quizCard = MakeCard(content.transform, "Тест сұрақтары (міндетті емес)", 300);
        quizInput = MakeInputField(quizCard.transform, "Сұрақ\nЖауап 1\n*Дұрыс жауап\nЖауап 3", 230, true);

        // ── URL модели ───────────────────────────────────────────────────────
        var modelCard = MakeCard(content.transform, "3D Модель URL (міндетті емес)", 170);
        modelUrlInput = MakeInputField(modelCard.transform, "https://... .glb", 100, false);

        // ── URL видео ────────────────────────────────────────────────────────
        var videoCard = MakeCard(content.transform, "Видео URL (міндетті емес)", 170);
        videoUrlInput = MakeInputField(videoCard.transform, "https://... .mp4", 100, false);

        // ── КНОПКИ ───────────────────────────────────────────────────────────
        var btnRow = new GameObject("BtnRow");
        btnRow.transform.SetParent(content.transform, false);
        btnRow.AddComponent<RectTransform>();
        var hlg = btnRow.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 24;
        hlg.childForceExpandWidth = true;
        hlg.childForceExpandHeight = true;
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;
        var btnLE = btnRow.AddComponent<LayoutElement>();
        btnLE.preferredHeight = 120;

        // «Қадам қосу»
        var addGO = MakeActionButton(btnRow.transform, "Қадам қосу +", accentColor);
        addStepBtn = addGO.GetComponent<Button>();
        addStepBtn.onClick.AddListener(OnNextStepClicked);

        // «Сақтау»
        var saveGO = MakeActionButton(btnRow.transform, "Сақтау", greenColor);
        saveBtn = saveGO.GetComponent<Button>();
        saveBtn.onClick.AddListener(OnSaveScenarioClicked);

        // ── Статус текст ─────────────────────────────────────────────────────
        statusText = MakeText("Status", content.transform, "", 36, greenColor, TextAlignmentOptions.Center);
        var statusLE = statusText.gameObject.AddComponent<LayoutElement>();
        statusLE.preferredHeight = 60;

        UpdateStepCounter();
    }

    // ═════════════════════════════════════════════════════════════════════════
    //  ЛОГИКА КОНСТРУКТОРА
    // ═════════════════════════════════════════════════════════════════════════
    void OnNextStepClicked()
    {
        if (!ValidateFields()) return;

        // Сохраняем название сценария
        if (!string.IsNullOrWhiteSpace(scenarioNameInput.text))
            ScenarioDraft.CurrentDraft.scenarioName = scenarioNameInput.text.Trim();

        CustomStep newStep = new CustomStep
        {
            title       = stepTitleInput.text.Trim(),
            description = descriptionInput.text.Trim(),
            warnings    = warningsInput.text.Trim(),
            modelUrl    = modelUrlInput.text.Trim(),
            videoUrl    = videoUrlInput.text.Trim(),
            quizRaw     = quizInput.text.Trim()
        };

        ScenarioDraft.CurrentDraft.steps.Add(newStep);
        ShowStatus($"✅ Қадам #{ScenarioDraft.CurrentDraft.steps.Count} қосылды!", greenColor);
        ClearStepFields();
        UpdateStepCounter();
    }

    void OnSaveScenarioClicked()
    {
        // Если поля заполнены — добавляем последний шаг
        if (!string.IsNullOrWhiteSpace(stepTitleInput.text))
            OnNextStepClicked();

        if (ScenarioDraft.CurrentDraft.steps.Count == 0)
        {
            ShowStatus("⚠️ Ең кемінде бір қадам қосыңыз!", orangeColor);
            return;
        }

        if (string.IsNullOrWhiteSpace(ScenarioDraft.CurrentDraft.scenarioName))
            ScenarioDraft.CurrentDraft.scenarioName = "Менің сценарийім";

        string jsonOutput = JsonUtility.ToJson(ScenarioDraft.CurrentDraft, true);
        Debug.Log("Сценарий JSON:\n" + jsonOutput);
        ShowStatus("📤 Серверге жіберілуде...", accentColor);
        StartCoroutine(SendScenarioToServer(jsonOutput));
    }

    IEnumerator SendScenarioToServer(string json)
    {
        string url = "https://autoreduce.kz/save_scenario.php";

        WWWForm form = new WWWForm();
        form.AddField("scenario_json", json);
        form.AddField("user_id", PlayerPrefs.GetInt("userId", 1).ToString());

        using (UnityWebRequest www = UnityWebRequest.Post(url, form))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                ShowStatus("✅ Сценарий сәтті сақталды!", greenColor);
                ScenarioDraft.CurrentDraft = new CustomScenario();
                yield return new WaitForSeconds(2f);
                SceneManager.LoadScene("Untitled");
            }
            else
            {
                ShowStatus("❌ Қате: " + www.error, redColor);
            }
        }
    }

    bool ValidateFields()
    {
        if (string.IsNullOrWhiteSpace(stepTitleInput.text))
        {
            ShowStatus("⚠️ Қадам тақырыбын жазыңыз!", orangeColor);
            return false;
        }
        if (string.IsNullOrWhiteSpace(descriptionInput.text))
        {
            ShowStatus("⚠️ Қадам сипаттамасын жазыңыз!", orangeColor);
            return false;
        }
        if (string.IsNullOrWhiteSpace(warningsInput.text))
        {
            ShowStatus("⚠️ Ескертулерді жазыңыз!", orangeColor);
            return false;
        }
        return true;
    }

    void ClearStepFields()
    {
        stepTitleInput.text = "";
        descriptionInput.text = "";
        warningsInput.text = "";
        quizInput.text = "";
        modelUrlInput.text = "";
        videoUrlInput.text = "";
        stepTitleInput.Select();
    }

    void UpdateStepCounter()
    {
        if (stepCounterText != null)
            stepCounterText.text = $"Қадамдар: {ScenarioDraft.CurrentDraft.steps.Count}";
    }

    void ShowStatus(string msg, Color color)
    {
        if (statusText != null)
        {
            statusText.text = msg;
            statusText.color = color;
        }
    }

    // ═════════════════════════════════════════════════════════════════════════
    //  UI УТИЛИТЫ
    // ═════════════════════════════════════════════════════════════════════════
    GameObject MakeCard(Transform parent, string label, float height)
    {
        var card = new GameObject("Card");
        card.transform.SetParent(parent, false);
        var cardImg = card.AddComponent<Image>();
        cardImg.color = cardColor;
        var le = card.AddComponent<LayoutElement>();
        le.preferredHeight = height;

        var cardVLG = card.AddComponent<VerticalLayoutGroup>();
        cardVLG.spacing = 8;
        cardVLG.padding = new RectOffset(24, 24, 16, 16);
        cardVLG.childForceExpandWidth = true;
        cardVLG.childForceExpandHeight = false;
        cardVLG.childControlWidth = true;
        cardVLG.childControlHeight = true;

        var labelTMP = MakeText("Label", card.transform, label, 36, subtextColor, TextAlignmentOptions.Left);
        var labelLE = labelTMP.gameObject.AddComponent<LayoutElement>();
        labelLE.preferredHeight = 50;

        return card;
    }

    TMP_InputField MakeInputField(Transform parent, string placeholder, float height, bool multiline)
    {
        var fieldGO = new GameObject("InputField");
        fieldGO.transform.SetParent(parent, false);
        var fieldImg = fieldGO.AddComponent<Image>();
        fieldImg.color = inputBgColor;
        var fieldLE = fieldGO.AddComponent<LayoutElement>();
        fieldLE.preferredHeight = height;
        fieldLE.flexibleWidth = 1;

        // Text Area внутри
        var textArea = new GameObject("TextArea");
        textArea.transform.SetParent(fieldGO.transform, false);
        var textAreaRT = textArea.AddComponent<RectTransform>();
        textAreaRT.anchorMin = Vector2.zero;
        textAreaRT.anchorMax = Vector2.one;
        textAreaRT.offsetMin = V2(20, 10);
        textAreaRT.offsetMax = V2(-20, -10);
        textArea.AddComponent<RectMask2D>();

        // Placeholder
        var phGO = new GameObject("Placeholder");
        phGO.transform.SetParent(textArea.transform, false);
        var phRT = phGO.AddComponent<RectTransform>();
        phRT.anchorMin = Vector2.zero;
        phRT.anchorMax = Vector2.one;
        phRT.offsetMin = phRT.offsetMax = Vector2.zero;
        var phTMP = phGO.AddComponent<TextMeshProUGUI>();
        phTMP.text = placeholder;
        phTMP.fontSize = 34;
        phTMP.color = placeholderCol;
        phTMP.fontStyle = FontStyles.Italic;
        phTMP.enableWordWrapping = true;
        phTMP.alignment = TextAlignmentOptions.TopLeft;
        if (customFont != null) phTMP.font = customFont;

        // Ввод текста
        var inputGO = new GameObject("Text");
        inputGO.transform.SetParent(textArea.transform, false);
        var inputRT = inputGO.AddComponent<RectTransform>();
        inputRT.anchorMin = Vector2.zero;
        inputRT.anchorMax = Vector2.one;
        inputRT.offsetMin = inputRT.offsetMax = Vector2.zero;
        var inputTMP = inputGO.AddComponent<TextMeshProUGUI>();
        inputTMP.fontSize = 34;
        inputTMP.color = textColor;
        inputTMP.enableWordWrapping = true;
        inputTMP.alignment = TextAlignmentOptions.TopLeft;
        if (customFont != null) inputTMP.font = customFont;

        // TMP_InputField
        var inputField = fieldGO.AddComponent<TMP_InputField>();
        inputField.textViewport = textAreaRT;
        inputField.textComponent = inputTMP;
        inputField.placeholder = phTMP;
        inputField.fontAsset = customFont;
        inputField.pointSize = 34;
        inputField.lineType = multiline
            ? TMP_InputField.LineType.MultiLineNewline
            : TMP_InputField.LineType.SingleLine;
        inputField.characterLimit = multiline ? 2000 : 500;

        // Выделение и каретка
        inputField.caretColor = accentColor;
        inputField.selectionColor = new Color(accentColor.r, accentColor.g, accentColor.b, 0.3f);

        return inputField;
    }

    GameObject MakeActionButton(Transform parent, string label, Color bgCol)
    {
        var go = new GameObject("ActionBtn");
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = bgCol;

        var txt = MakeText("Txt", go.transform, label, 40, Color.white, TextAlignmentOptions.Center);
        SetRect(txt.gameObject, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        return go;
    }

    GameObject MakeButtonObj(string name, Transform parent, string label, float fontSize, Color bgCol)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        var img = go.AddComponent<Image>();
        img.color = bgCol;

        var txt = MakeText("Txt", go.transform, label, fontSize, Color.white, TextAlignmentOptions.Center);
        SetRect(txt.gameObject, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        go.AddComponent<Button>().targetGraphic = img;
        return go;
    }

    GameObject MakePanel(string name, Transform parent, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<Image>().color = color;
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        return go;
    }

    TextMeshProUGUI MakeText(string name, Transform parent, string text, float size, Color color, TextAlignmentOptions align)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text; tmp.fontSize = size; tmp.color = color;
        tmp.alignment = align; tmp.enableWordWrapping = true;
        if (customFont != null) tmp.font = customFont;
        return tmp;
    }

    void SetRect(GameObject go, Vector2 aMin, Vector2 aMax, Vector2 oMin, Vector2 oMax)
    {
        var rt = go.GetComponent<RectTransform>() ?? go.AddComponent<RectTransform>();
        rt.anchorMin = aMin; rt.anchorMax = aMax; rt.offsetMin = oMin; rt.offsetMax = oMax;
    }

    void SetAnchors(RectTransform rt, Vector2 aMin, Vector2 aMax, Vector2 oMin, Vector2 oMax)
    {
        rt.anchorMin = aMin; rt.anchorMax = aMax; rt.offsetMin = oMin; rt.offsetMax = oMax;
    }

    Vector2 V2(float x, float y) => new Vector2(x, y);
}
