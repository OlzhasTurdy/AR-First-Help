using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Builds the renewed theory screen at runtime and hides the old scene canvas.
/// Put it on any GameObject, or leave it as-is: it auto-runs in the Theory scene.
/// </summary>
public class TheoryScreenBuilder : MonoBehaviour
{
    [Header("Existing UI")]
    public Canvas existingCanvas;
    public GameObject existingUI;

    [Header("Navigation")]
    public string backSceneName = "selectiontheory";

    [Header("Font")]
    public TMP_FontAsset customFont;

    private static readonly string[] AutoBuildSceneNames = { "Theory" };

    private readonly Color bgColor = new Color(0.055f, 0.065f, 0.095f, 1f);
    private readonly Color headerColor = new Color(0.09f, 0.115f, 0.18f, 1f);
    private readonly Color panelColor = new Color(0.115f, 0.135f, 0.19f, 1f);
    private readonly Color cardColor = new Color(0.145f, 0.165f, 0.225f, 1f);
    private readonly Color accentColor = new Color(0.18f, 0.58f, 1f, 1f);
    private readonly Color greenColor = new Color(0.18f, 0.76f, 0.48f, 1f);
    private readonly Color yellowColor = new Color(0.98f, 0.69f, 0.25f, 1f);
    private readonly Color redColor = new Color(0.90f, 0.28f, 0.30f, 1f);
    private readonly Color textColor = new Color(0.94f, 0.95f, 0.98f, 1f);
    private readonly Color subtextColor = new Color(0.67f, 0.70f, 0.78f, 1f);

    private Canvas mainCanvas;
    private RectTransform tabContent;
    private RectTransform articleContent;
    private ScrollRect articleScroll;
    private TextMeshProUGUI headerTitle;
    private TextMeshProUGUI headerSubtitle;
    private readonly List<Button> tabButtons = new List<Button>();
    private readonly List<TheoryTopic> topics = new List<TheoryTopic>();
    private int activeTopicIndex;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void RegisterAutoBuilder()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        TryBuildForScene(SceneManager.GetActiveScene());
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        TryBuildForScene(scene);
    }

    private static void TryBuildForScene(Scene scene)
    {
        if (!ShouldAutoBuild(scene.name)) return;
        if (FindObjectOfType<TheoryScreenBuilder>() != null) return;

        var go = new GameObject("TheoryScreenBuilder_Runtime");
        go.AddComponent<TheoryScreenBuilder>();
    }

    private static bool ShouldAutoBuild(string sceneName)
    {
        for (int i = 0; i < AutoBuildSceneNames.Length; i++)
        {
            if (sceneName == AutoBuildSceneNames[i]) return true;
        }
        return false;
    }

    private void Start()
    {
        BuildUI();
    }

    private void BuildUI()
    {
        PrepareTopics();
        HideOldUI();
        EnsureCanvas();
        EnsureEventSystem();
        BuildScreen();
        SelectTopic(0);
    }

    private void HideOldUI()
    {
        if (existingUI != null) existingUI.SetActive(false);

        if (existingCanvas != null)
        {
            existingCanvas.gameObject.SetActive(false);
            return;
        }

        Canvas[] canvases = FindObjectsOfType<Canvas>();
        for (int i = 0; i < canvases.Length; i++)
        {
            if (canvases[i].GetComponentInParent<TheoryScreenBuilder>() == null)
                canvases[i].gameObject.SetActive(false);
        }
    }

    private void EnsureCanvas()
    {
        var go = new GameObject("TheoryCanvas");
        go.transform.SetParent(transform, false);

        mainCanvas = go.AddComponent<Canvas>();
        mainCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        mainCanvas.sortingOrder = 200;

        var scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 2400);
        scaler.matchWidthOrHeight = 0.5f;

        go.AddComponent<GraphicRaycaster>();
    }

    private void EnsureEventSystem()
    {
        if (FindObjectOfType<EventSystem>() != null) return;

        var eventSystem = new GameObject("EventSystem");
        eventSystem.AddComponent<EventSystem>();
        eventSystem.AddComponent<StandaloneInputModule>();
    }

    private void BuildScreen()
    {
        var root = MakePanel("Root", mainCanvas.transform, bgColor);

        var header = MakePanel("Header", root.transform, headerColor);
        SetRect(header, V2(0, 1), V2(1, 1), V2(0, -250), V2(0, 0));

        var backButton = MakeButton("BackButton", header.transform, "<", 56, redColor);
        SetRect(backButton.gameObject, V2(0, 0.5f), V2(0, 0.5f), V2(28, -48), V2(132, 48));
        backButton.onClick.AddListener(() => SceneManager.LoadScene(backSceneName));

        headerTitle = MakeText("Title", header.transform, "Алғашқы көмек теориясы", 50, textColor, TextAlignmentOptions.Left);
        SetRect(headerTitle.gameObject, V2(0, 0.44f), V2(1, 1), V2(172, 0), V2(-36, -18));

        headerSubtitle = MakeText("Subtitle", header.transform, "", 30, subtextColor, TextAlignmentOptions.Left);
        SetRect(headerSubtitle.gameObject, V2(0, 0), V2(1, 0.48f), V2(172, 18), V2(-36, -8));

        var tabsScrollGO = new GameObject("TabsScroll");
        tabsScrollGO.transform.SetParent(root.transform, false);
        var tabsRT = tabsScrollGO.AddComponent<RectTransform>();
        SetAnchors(tabsRT, V2(0, 1), V2(1, 1), V2(28, -390), V2(-28, -270));
        var tabsScroll = tabsScrollGO.AddComponent<ScrollRect>();
        tabsScroll.vertical = false;
        tabsScroll.horizontal = true;
        tabsScrollGO.AddComponent<RectMask2D>();

        var tabsContentGO = new GameObject("TabsContent");
        tabsContentGO.transform.SetParent(tabsScrollGO.transform, false);
        tabContent = tabsContentGO.AddComponent<RectTransform>();
        tabContent.anchorMin = V2(0, 0);
        tabContent.anchorMax = V2(0, 1);
        tabContent.pivot = V2(0, 0.5f);
        tabContent.offsetMin = Vector2.zero;
        tabContent.offsetMax = Vector2.zero;

        var tabsLayout = tabsContentGO.AddComponent<HorizontalLayoutGroup>();
        tabsLayout.spacing = 16;
        tabsLayout.padding = new RectOffset(0, 0, 12, 12);
        tabsLayout.childControlWidth = true;
        tabsLayout.childControlHeight = true;
        tabsLayout.childForceExpandWidth = false;
        tabsLayout.childForceExpandHeight = true;

        var tabsFitter = tabsContentGO.AddComponent<ContentSizeFitter>();
        tabsFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        tabsScroll.content = tabContent;

        for (int i = 0; i < topics.Count; i++)
        {
            CreateTopicTab(i);
        }

        var scrollGO = new GameObject("ArticleScroll");
        scrollGO.transform.SetParent(root.transform, false);
        var scrollRT = scrollGO.AddComponent<RectTransform>();
        SetAnchors(scrollRT, V2(0, 0), V2(1, 1), V2(28, 26), V2(-28, -410));
        articleScroll = scrollGO.AddComponent<ScrollRect>();
        articleScroll.horizontal = false;
        scrollGO.AddComponent<RectMask2D>();

        var articleGO = new GameObject("ArticleContent");
        articleGO.transform.SetParent(scrollGO.transform, false);
        articleContent = articleGO.AddComponent<RectTransform>();
        articleContent.anchorMin = V2(0, 1);
        articleContent.anchorMax = V2(1, 1);
        articleContent.pivot = V2(0.5f, 1);
        articleContent.offsetMin = Vector2.zero;
        articleContent.offsetMax = Vector2.zero;

        var articleLayout = articleGO.AddComponent<VerticalLayoutGroup>();
        articleLayout.spacing = 22;
        articleLayout.padding = new RectOffset(0, 0, 0, 48);
        articleLayout.childControlWidth = true;
        articleLayout.childControlHeight = true;
        articleLayout.childForceExpandWidth = true;
        articleLayout.childForceExpandHeight = false;

        var articleFitter = articleGO.AddComponent<ContentSizeFitter>();
        articleFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        articleScroll.content = articleContent;
    }

    private void CreateTopicTab(int index)
    {
        var topic = topics[index];
        var button = MakeButton("Tab_" + topic.ShortTitle, tabContent, topic.ShortTitle, 30, cardColor);
        var layout = button.gameObject.AddComponent<LayoutElement>();
        layout.preferredWidth = Mathf.Max(190, topic.ShortTitle.Length * 24 + 70);
        layout.preferredHeight = 88;
        int captured = index;
        button.onClick.AddListener(() => SelectTopic(captured));
        tabButtons.Add(button);
    }

    private void SelectTopic(int index)
    {
        activeTopicIndex = Mathf.Clamp(index, 0, topics.Count - 1);
        var topic = topics[activeTopicIndex];

        headerTitle.text = topic.Title;
        headerSubtitle.text = topic.Subtitle;

        for (int i = 0; i < tabButtons.Count; i++)
        {
            var image = tabButtons[i].GetComponent<Image>();
            image.color = i == activeTopicIndex ? accentColor : cardColor;
        }

        ClearChildren(articleContent);
        CreateHeroCard(topic);
        CreateQuickGrid(topic);
        CreateInfoSection("Белгілері", topic.Signs, yellowColor);
        CreateInfoSection("Қадамдары", topic.Steps, greenColor);
        CreateInfoSection("Ескерту", topic.Warnings, redColor);

        Canvas.ForceUpdateCanvases();
        articleScroll.verticalNormalizedPosition = 1f;
    }

    private void CreateHeroCard(TheoryTopic topic)
    {
        var hero = MakeCard("HeroCard", articleContent, 300, panelColor);

        var title = MakeText("HeroTitle", hero.transform, topic.Title, 42, textColor, TextAlignmentOptions.Left);
        title.fontStyle = FontStyles.Bold;
        title.lineSpacing = 4;
        title.gameObject.AddComponent<LayoutElement>().preferredHeight = 64;

        var body = MakeText("HeroBody", hero.transform, topic.Intro, 31, subtextColor, TextAlignmentOptions.Left);
        body.lineSpacing = 8;
        var bodyLayout = body.gameObject.AddComponent<LayoutElement>();
        bodyLayout.preferredHeight = 138;

        var chipRow = new GameObject("ChipRow");
        chipRow.transform.SetParent(hero.transform, false);
        chipRow.AddComponent<RectTransform>();
        var rowLayout = chipRow.AddComponent<HorizontalLayoutGroup>();
        rowLayout.spacing = 12;
        rowLayout.childControlWidth = true;
        rowLayout.childControlHeight = true;
        rowLayout.childForceExpandWidth = true;
        rowLayout.childForceExpandHeight = true;
        chipRow.AddComponent<LayoutElement>().preferredHeight = 62;

        CreateSmallChip(chipRow.transform, "112 шақыру", redColor);
        CreateSmallChip(chipRow.transform, "Қауіпсіздік", accentColor);
        CreateSmallChip(chipRow.transform, topic.Priority, greenColor);
    }

    private void CreateQuickGrid(TheoryTopic topic)
    {
        var gridCard = MakeCard("QuickGrid", articleContent, 250, panelColor);

        var title = MakeText("QuickTitle", gridCard.transform, "Жылдам бағдар", 34, textColor, TextAlignmentOptions.Left);
        title.fontStyle = FontStyles.Bold;
        title.gameObject.AddComponent<LayoutElement>().preferredHeight = 48;

        var grid = new GameObject("Grid");
        grid.transform.SetParent(gridCard.transform, false);
        grid.AddComponent<RectTransform>();
        var layout = grid.AddComponent<GridLayoutGroup>();
        layout.cellSize = new Vector2(486, 76);
        layout.spacing = new Vector2(18, 16);
        layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        layout.constraintCount = 2;
        grid.AddComponent<LayoutElement>().preferredHeight = 170;

        for (int i = 0; i < topic.QuickFacts.Length; i++)
        {
            var item = MakePanel("QuickItem", grid.transform, cardColor);
            var text = MakeText("Text", item.transform, topic.QuickFacts[i], 27, textColor, TextAlignmentOptions.Left);
            SetRect(text.gameObject, Vector2.zero, Vector2.one, V2(18, 8), V2(-12, -8));
        }
    }

    private void CreateInfoSection(string titleText, string[] lines, Color color)
    {
        var section = MakeCard("Section_" + titleText, articleContent, 120 + lines.Length * 82, panelColor);

        var header = new GameObject("SectionHeader");
        header.transform.SetParent(section.transform, false);
        header.AddComponent<RectTransform>();
        var headerLayout = header.AddComponent<HorizontalLayoutGroup>();
        headerLayout.spacing = 14;
        headerLayout.childControlWidth = true;
        headerLayout.childControlHeight = true;
        headerLayout.childForceExpandWidth = false;
        headerLayout.childForceExpandHeight = true;
        header.AddComponent<LayoutElement>().preferredHeight = 58;

        var mark = MakePanel("Mark", header.transform, color);
        var markLayout = mark.AddComponent<LayoutElement>();
        markLayout.preferredWidth = 16;
        markLayout.preferredHeight = 54;

        var title = MakeText("Title", header.transform, titleText, 36, textColor, TextAlignmentOptions.Left);
        title.fontStyle = FontStyles.Bold;
        title.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;

        for (int i = 0; i < lines.Length; i++)
        {
            var row = MakePanel("Row", section.transform, cardColor);
            row.AddComponent<LayoutElement>().preferredHeight = 74;

            var number = MakeText("Number", row.transform, (i + 1).ToString(), 28, color, TextAlignmentOptions.Center);
            number.fontStyle = FontStyles.Bold;
            SetRect(number.gameObject, V2(0, 0), V2(0, 1), V2(12, 0), V2(70, 0));

            var line = MakeText("Line", row.transform, lines[i], 29, textColor, TextAlignmentOptions.Left);
            line.lineSpacing = 4;
            SetRect(line.gameObject, V2(0, 0), V2(1, 1), V2(82, 8), V2(-18, -8));
        }
    }

    private void CreateSmallChip(Transform parent, string label, Color color)
    {
        var chip = MakePanel("Chip", parent, color);
        chip.AddComponent<LayoutElement>().preferredHeight = 58;
        var text = MakeText("Text", chip.transform, label, 26, Color.white, TextAlignmentOptions.Center);
        SetRect(text.gameObject, Vector2.zero, Vector2.one, V2(10, 0), V2(-10, 0));
    }

    private GameObject MakeCard(string name, Transform parent, float height, Color color)
    {
        var card = MakePanel(name, parent, color);
        var layout = card.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 14;
        layout.padding = new RectOffset(24, 24, 22, 22);
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        var layoutElement = card.AddComponent<LayoutElement>();
        layoutElement.minHeight = height;
        layoutElement.preferredHeight = height;
        return card;
    }

    private Button MakeButton(string name, Transform parent, string label, float size, Color color)
    {
        var go = MakePanel(name, parent, color);
        var text = MakeText("Text", go.transform, label, size, Color.white, TextAlignmentOptions.Center);
        text.fontStyle = FontStyles.Bold;
        SetRect(text.gameObject, Vector2.zero, Vector2.one, V2(10, 0), V2(-10, 0));

        var button = go.AddComponent<Button>();
        button.targetGraphic = go.GetComponent<Image>();
        return button;
    }

    private GameObject MakePanel(string name, Transform parent, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var image = go.AddComponent<Image>();
        image.color = color;
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        return go;
    }

    private TextMeshProUGUI MakeText(string name, Transform parent, string text, float size, Color color, TextAlignmentOptions align)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();

        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.color = color;
        tmp.alignment = align;
        tmp.enableWordWrapping = true;
        tmp.overflowMode = TextOverflowModes.Ellipsis;
        if (customFont != null) tmp.font = customFont;
        return tmp;
    }

    private void ClearChildren(Transform parent)
    {
        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            Destroy(parent.GetChild(i).gameObject);
        }
    }

    private void SetRect(GameObject go, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        var rect = go.GetComponent<RectTransform>() ?? go.AddComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
    }

    private void SetAnchors(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
    }

    private Vector2 V2(float x, float y)
    {
        return new Vector2(x, y);
    }

    private void PrepareTopics()
    {
        if (topics.Count > 0) return;

        topics.Add(new TheoryTopic(
            "Базалық алғашқы көмек",
            "Қауіпсіздік, 112, жағдайды бағалау",
            "Негізгі мақсат - өзіңізді қауіпке салмай, зардап шегушінің өміріне төнген қатерді тез анықтау және жедел жәрдем келгенше қарапайым көмек көрсету.",
            "Алғашқы 1 минут",
            new[] { "Қауіп жоқ па?", "Есін тексеру", "112 шақыру", "Қан кетуді тоқтату" },
            new[] { "Оқиға орнында от, ток, көлік, газ, су немесе агрессия сияқты қауіп бар-жоғын бағалаңыз.", "Зардап шегушіге жақындап, қатты дауыспен сөйлеп, иығынан жеңіл түртіңіз.", "Дем алуы жоқ, қатты қан кету, кеуде ауыруы, инсульт белгісі немесе ауыр жарақат болса - 112 шақырыңыз." },
            new[] { "Қауіпсіз жерге көшіріңіз немесе қауіпті тоқтатыңыз.", "Есін және тынысын тексеріңіз.", "112-ге нақты мекенжай, жағдай, зардап шегушілер саны және өз нөміріңізді айтыңыз.", "Қатты қан кетсе, жараға тікелей қысым жасаңыз.", "Дем алуы бар ессіз адамды қырынан жатқызып, тынысын бақылаңыз." },
            new[] { "Өзіңізге қауіп төнсе, жақындамаңыз.", "Ессіз адамға су, дәрі немесе тамақ бермеңіз.", "Мойын немесе омыртқа жарақаты күдігінде адамды қажетсіз қозғалтпаңыз." }));

        topics.Add(new TheoryTopic(
            "ЖӨР / СЛР",
            "Жүрек-өкпе реанимациясы",
            "ЖӨР адамның тынысы жоқ немесе қалыпты емес кезде қан айналымын уақытша ұстап тұруға көмектеседі. Ең маңыздысы - 112 шақыру және кеудені үздіксіз басу.",
            "30 басу",
            new[] { "Тыныс жоқ", "Агоналды тыныс", "Кеуде ортасы", "100-120/мин" },
            new[] { "Адам жауап бермейді.", "10 секунд ішінде қалыпты тыныс байқалмайды.", "Сирек, қорыл тәрізді немесе үзік тыныс қалыпты тыныс болып саналмайды." },
            new[] { "112 шақырыңыз және айналадағылардан AED іздеуді сұраңыз.", "Адамды қатты жерге шалқасынан жатқызыңыз.", "Қолды кеуденің ортасына қойып, 5-6 см тереңдікке басыңыз.", "Минутына 100-120 рет ырғақпен басыңыз.", "Үйретілген болсаңыз, 30 басудан кейін 2 үрлеу жасаңыз." },
            new[] { "Кеудені басуды ұзақ тоқтатпаңыз.", "Қалыпты тыныс пайда болмаса, жедел жәрдем келгенше жалғастырыңыз.", "Балаларда қысым тереңдігі кеуде қалыңдығының шамамен үштен бірі." }));

        topics.Add(new TheoryTopic(
            "Инсульт",
            "Бет, қол, сөйлеу, уақыт",
            "Инсульт кезінде миға қан баруы бұзылады. Уақыт өте маңызды: белгілер басталған нақты уақытты есте сақтап, тез арада 112 шақыру керек.",
            "Уақыт маңызды",
            new[] { "Бет қисайды", "Қол әлсіреді", "Сөйлеу бұзылды", "Басталу уақыты" },
            new[] { "Беттің бір жағы салбырап немесе күлімдегенде тең емес көрінеді.", "Бір қол немесе аяқ кенет әлсірейді, жансызданады.", "Сөйлеуі түсініксіз, сөз таба алмайды немесе түсінбейді.", "Кенет қатты бас ауыру, көрудің бұзылуы, тепе-теңдікті жоғалту болуы мүмкін." },
            new[] { "112 шақырыңыз.", "Белгілер басталған уақытты белгілеңіз.", "Адамды отырғызып немесе басын сәл көтеріп жатқызыңыз.", "Тынысын бақылаңыз, есінен танса қырынан жатқызыңыз.", "Дәрігер келгенше жанында болыңыз." },
            new[] { "Аспирин немесе басқа дәріні өз бетіңізше бермеңіз.", "Тамақ, су, шай бермеңіз - жұтынуы бұзылуы мүмкін.", "Белгілер өтіп кеткендей болса да, жедел көмек қажет." }));

        topics.Add(new TheoryTopic(
            "Инфаркт",
            "Кеуде ауыруы және ентігу",
            "Инфаркт жүрек бұлшықетіне қан жетпегенде болады. Кеуде қысылуы, суық тер, әлсіздік немесе тыныс тарылуы байқалса, уақыт жоғалтпай 112 шақырыңыз.",
            "Отырғызу",
            new[] { "Кеуде қысымы", "Сол қолға таралу", "Суық тер", "Ентігу" },
            new[] { "Кеудеде қысу, күйдіру немесе ауырлық сезімі.", "Ауырсыну сол қолға, арқаға, мойынға немесе жаққа тарауы мүмкін.", "Суық тер, жүрек айну, қорқыныш, қатты әлсіздік.", "Әйелдерде, қарттарда және диабеті бар адамдарда белгісі әлсіз болуы мүмкін." },
            new[] { "112 шақырыңыз.", "Адамды жартылай отырғызып, тыныштандырыңыз.", "Киімін босатып, таза ауа кіргізіңіз.", "Өзінің дәрігер жазған нитроглицерині болса ғана қолдануына көмектесіңіз.", "Тынысы тоқтаса, ЖӨР бастаңыз." },
            new[] { "Адамды жүруге немесе көлікпен өзі баруға мәжбүрлемеңіз.", "Дәріні өз бетіңізше ұсынбаңыз.", "Ауру басылды деп жедел көмектен бас тартпаңыз." }));

        topics.Add(new TheoryTopic(
            "Суға кеткен адам",
            "Судан шығару, тыныс, жылу",
            "Суға кеткен адамға көмек көрсеткенде бірінші қауіпсіздік маңызды. Суға өзіңіз секірмей, мүмкін болса құралмен тартыңыз, кейін тынысын тексеріңіз.",
            "Тыныс тексеру",
            new[] { "Қауіпсіз шығару", "Тыныс бар ма?", "ЖӨР қажет", "Жылыту" },
            new[] { "Адам суда қалқып жүр немесе су жұтқаннан кейін әлсіз.", "Жөтел, көгеру, тыныс тарылуы байқалады.", "Есі жоқ немесе қалыпты тыныс жоқ." },
            new[] { "Өзіңізді қауіпке салмай, адамды судан шығарыңыз.", "112 шақырыңыз.", "Ауыз қуысын көрінетін бөгде заттан ғана тазалаңыз.", "Тынысы жоқ болса, ЖӨР бастаңыз.", "Тынысы бар болса, қырынан жатқызып, дымқыл киімді шешіп, жылытыңыз." },
            new[] { "Суды өкпеден шығарам деп адамды төңкермеңіз.", "Жөтелі басылса да, су жұтқан адамды дәрігер қарауы керек.", "Суық суда болған адамды күрт қыздырмаңыз." }));

        topics.Add(new TheoryTopic(
            "Улану",
            "У, дәрі, газ, химиялық зат",
            "Улануда бастысы - удың түрін анықтау, әсерін тоқтату және 112-ден нұсқаулық алу. Қаптаманы, дәрі атауын немесе химиялық затты сақтап қойыңыз.",
            "У көзін тоқтату",
            new[] { "Жүрек айну", "Бас айналу", "Тыныс қиындау", "Ес бұзылу" },
            new[] { "Құсу, іш ауыру, әлсіздік, тершеңдік.", "Тыныс тарылуы, жөтел, көз ашуы немесе тері күйігі.", "Ұйқышылдық, шатасу, есінен тану.", "Газдан улануда бірнеше адамда бірдей белгі болуы мүмкін." },
            new[] { "112 шақырыңыз.", "Газ болса, адамды таза ауаға шығарып, терезені ашыңыз.", "Химиялық зат теріге тисе, ластанған киімді шешіп, сумен шайыңыз.", "Удың қаптамасын немесе дәрі атауын дәрігерге көрсетіңіз.", "Ессіз болса, тынысын бақылап, қырынан жатқызыңыз." },
            new[] { "Дәрігер айтпаса, құстырмаңыз.", "Сүт, алкоголь, тамақ немесе дәрі бермеңіз.", "Газ иісі болса, электр қосқыштарын баспаңыз және от жақпаңыз." }));

        topics.Add(new TheoryTopic(
            "Жануар шабуылы",
            "Тістеу, тырнау, қан кету",
            "Жануар шабуылынан кейін жараны тез өңдеу, қан кетуді тоқтату және инфекция қаупін бағалау керек. Құтыру қаупі болса, медициналық көмек міндетті.",
            "Жараны шаю",
            new[] { "Тістеу", "Қан кету", "Ісіну", "Құтыру қаупі" },
            new[] { "Тістелген немесе тырналған жара.", "Қан кету, ауырсыну, ісіну.", "Жануар белгісіз, жабайы немесе мінезі күмәнді.", "Бет, мойын, қол саусақтары жарақаттанса, қауіп жоғары." },
            new[] { "Жануардан алыстап, қауіпсіз жерге шығыңыз.", "Қатты қан кетсе, таза матамен қысым жасаңыз.", "Жараны ағын сумен және сабынмен бірнеше минут жуыңыз.", "Таза таңғыш қойыңыз.", "112 немесе жақын медициналық пунктке хабарласыңыз." },
            new[] { "Жараны ауызбен соруға болмайды.", "Терең жараны өзіңіз жапсырмаңыз.", "Құтыруға қарсы екпе уақытында басталуы керек." }));

        topics.Add(new TheoryTopic(
            "Жол апаты",
            "Қауіпсіздік, жарақат, 112",
            "Жол апатында алдымен қозғалыс пен өрт қаупін бақылау керек. Зардап шегушіні тек нақты қауіп болса ғана көшіреді, себебі омыртқа жарақаты болуы мүмкін.",
            "Оқиға орнын қорғау",
            new[] { "Көлік қозғалысы", "Қан кету", "Ессіздік", "Омыртқа қаупі" },
            new[] { "Адам көлікте қысылып қалуы мүмкін.", "Қатты қан кету, сыну, күйік немесе бас жарақаты.", "Есі шатасқан, есінен танған немесе тынысы қиындаған.", "Мойын, арқа ауыруы, аяқ-қол сезбеуі омыртқа жарақатын білдіруі мүмкін." },
            new[] { "Өзіңізді қауіпсіз жерге қойып, апат белгісін қосыңыз.", "112 шақырып, орын, көлік саны және зардап шегушілер туралы айтыңыз.", "Қатты қан кетуді тікелей қысыммен тоқтатыңыз.", "Тынысын бақылаңыз, қажет болса ЖӨР бастаңыз.", "Өрт, суға бату немесе жарылыс қаупі болмаса, адамды қозғалтпаңыз." },
            new[] { "Шлемді себепсіз шешпеңіз.", "Жарақаттанған адамға су немесе тамақ бермеңіз.", "Көлік астындағы немесе қысылған адамды күшпен тартпаңыз." }));
    }

    private class TheoryTopic
    {
        public readonly string Title;
        public readonly string ShortTitle;
        public readonly string Subtitle;
        public readonly string Intro;
        public readonly string Priority;
        public readonly string[] QuickFacts;
        public readonly string[] Signs;
        public readonly string[] Steps;
        public readonly string[] Warnings;

        public TheoryTopic(string title, string subtitle, string intro, string priority, string[] quickFacts, string[] signs, string[] steps, string[] warnings)
        {
            Title = title;
            ShortTitle = title;
            Subtitle = subtitle;
            Intro = intro;
            Priority = priority;
            QuickFacts = quickFacts;
            Signs = signs;
            Steps = steps;
            Warnings = warnings;
        }
    }
}
