using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

/// <summary>
/// LibraryScreenBuilder — программно создаёт экран библиотеки и экран чтения книги.
/// Повесьте на пустой GameObject в любой сцене.
/// Книги загружаются из Resources/Books/books_catalog.json.
/// Обложка: Resources/Books/book_cover_universal (Sprite).
/// </summary>
public class LibraryScreenBuilder : MonoBehaviour
{
    [Header("Шрифт (опционально)")]
    public TMP_FontAsset customFont;

    [Header("Существующий Canvas (перетащите из сцены — если пусто, создастся новый)")]
    public Canvas existingCanvas;

    [Header("Старый UI (скроется когда скрипт запустится)")]
    public GameObject existingUI;

    [Header("Навигация")]
    public string backSceneName = "Untitled";

    [Header("Настройки")]
    public bool showOnStart = true;

    // ── Цвета ────────────────────────────────────────────────────────────────
    private readonly Color bgColor      = new Color(0.06f, 0.07f, 0.10f, 1f);
    private readonly Color headerColor  = new Color(0.10f, 0.12f, 0.18f, 1f);
    private readonly Color cardBgColor  = new Color(0.13f, 0.14f, 0.19f, 1f);
    private readonly Color accentColor  = new Color(0.25f, 0.60f, 1.0f, 1f);
    private readonly Color textColor    = new Color(0.93f, 0.93f, 0.95f, 1f);
    private readonly Color subtextColor = new Color(0.55f, 0.56f, 0.62f, 1f);
    private readonly Color readerBg     = new Color(0.97f, 0.96f, 0.92f, 1f);
    private readonly Color readerText   = new Color(0.15f, 0.13f, 0.10f, 1f);
    private readonly Color backBtnColor = new Color(0.85f, 0.25f, 0.25f, 1f);

    // ── Внутреннее ───────────────────────────────────────────────────────────
    private Canvas          mainCanvas;
    private GameObject      libraryScreen;
    private GameObject      readerScreen;
    private Sprite          coverSprite;

    private List<string>    pages       = new List<string>();
    private int             currentPage = 0;
    private TextMeshProUGUI readerPageText;
    private TextMeshProUGUI readerCounterText;
    private TextMeshProUGUI readerTitleText;
    private Button          readerPrevBtn;
    private Button          readerNextBtn;

    // ── JSON ─────────────────────────────────────────────────────────────────
    [System.Serializable] private class BookMeta    { public string fileName, title, author, coverColor; }
    [System.Serializable] private class BookCatalog { public List<BookMeta> books; }
    [System.Serializable] private class BookData    { public string title, author, content; public int charsPerPage = 600; }

    // ═════════════════════════════════════════════════════════════════════════
    void Start()
    {
        // Загружаем универсальную обложку
        coverSprite = Resources.Load<Sprite>("Books/book_cover_universal");
        if (coverSprite == null)
        {
            // Попробуем как Texture2D и конвертируем
            var tex = Resources.Load<Texture2D>("Books/book_cover_universal");
            if (tex != null)
                coverSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
        }

        if (showOnStart) ShowLibrary();
    }

    public void ShowLibrary()
    {
        if (mainCanvas == null) BuildCanvas();
        if (libraryScreen == null) BuildLibraryScreen();
        if (existingUI != null) existingUI.SetActive(false);

        libraryScreen.SetActive(true);
        if (readerScreen != null) readerScreen.SetActive(false);
    }

    // ═════════════════════════════════════════════════════════════════════════
    //  CANVAS
    // ═════════════════════════════════════════════════════════════════════════
    void BuildCanvas()
    {
        if (existingCanvas != null) { mainCanvas = existingCanvas; return; }

        var go = new GameObject("LibraryCanvas");
        go.transform.SetParent(transform);
        mainCanvas = go.AddComponent<Canvas>();
        mainCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        mainCanvas.sortingOrder = 100;
        var scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 2400);
        scaler.matchWidthOrHeight = 0.5f;
        go.AddComponent<GraphicRaycaster>();
    }

    // ═════════════════════════════════════════════════════════════════════════
    //  БИБЛИОТЕКА
    // ═════════════════════════════════════════════════════════════════════════
    void BuildLibraryScreen()
    {
        libraryScreen = MakePanel("LibraryScreen", mainCanvas.transform, bgColor);

        // ── Хедер ────────────────────────────────────────────────────────────
        var header = MakePanel("Header", libraryScreen.transform, headerColor);
        SetRect(header, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -140), new Vector2(0, 0));

        // Кнопка «← Артқа»
        var backBtnGO = new GameObject("BackBtn");
        backBtnGO.transform.SetParent(header.transform, false);
        var backImg = backBtnGO.AddComponent<Image>();
        backImg.color = backBtnColor;
        SetRect(backBtnGO, new Vector2(0, 0.5f), new Vector2(0, 0.5f),
                new Vector2(20, -30), new Vector2(130, 30));
        var backTMP = MakeText("BackTxt", backBtnGO.transform, "←", 48, Color.white, TextAlignmentOptions.Center);
        SetRect(backTMP.gameObject, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        var backBtn = backBtnGO.AddComponent<Button>();
        backBtn.targetGraphic = backImg;
        backBtn.onClick.AddListener(() => SceneManager.LoadScene(backSceneName));

        // Заголовок
        var title = MakeText("Title", header.transform, "Кітапхана", 56, textColor, TextAlignmentOptions.Center);
        SetRect(title.gameObject, new Vector2(0.15f, 0), new Vector2(0.85f, 1), Vector2.zero, Vector2.zero);

        // ── Скролл ───────────────────────────────────────────────────────────
        var scrollGO = new GameObject("Scroll");
        scrollGO.transform.SetParent(libraryScreen.transform, false);
        var scrollRT = scrollGO.AddComponent<RectTransform>();
        SetAnchors(scrollRT, Vector2.zero, Vector2.one, new Vector2(30, 30), new Vector2(-30, -155));
        var scrollRect = scrollGO.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollGO.AddComponent<RectMask2D>();

        var content = new GameObject("Content");
        content.transform.SetParent(scrollGO.transform, false);
        var contentRT = content.AddComponent<RectTransform>();
        contentRT.anchorMin = new Vector2(0, 1);
        contentRT.anchorMax = new Vector2(1, 1);
        contentRT.pivot = new Vector2(0.5f, 1);
        contentRT.offsetMin = contentRT.offsetMax = Vector2.zero;

        var vlg = content.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 20;
        vlg.padding = new RectOffset(0, 0, 10, 40);
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;
        var csf = content.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        scrollRect.content = contentRT;

        // ── Карточки ─────────────────────────────────────────────────────────
        var asset = Resources.Load<TextAsset>("Books/books_catalog");
        if (asset == null) { Debug.LogError("[LibraryBuilder] books_catalog.json не найден!"); return; }
        var catalog = JsonUtility.FromJson<BookCatalog>(asset.text);
        if (catalog?.books == null) return;

        foreach (var meta in catalog.books)
            CreateBookCard(content.transform, meta);
    }

    void CreateBookCard(Transform parent, BookMeta meta)
    {
        var card = new GameObject("Card_" + meta.fileName);
        card.transform.SetParent(parent, false);
        var cardImg = card.AddComponent<Image>();
        cardImg.color = cardBgColor;
        var le = card.AddComponent<LayoutElement>();
        le.minHeight = le.preferredHeight = 200;

        var hlg = card.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 16;
        hlg.padding = new RectOffset(16, 16, 16, 16);
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = true;
        hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;

        // ── Обложка (универсальная картинка) ─────────────────────────────────
        var coverGO = new GameObject("Cover");
        coverGO.transform.SetParent(card.transform, false);
        var coverImg = coverGO.AddComponent<Image>();
        if (coverSprite != null)
        {
            coverImg.sprite = coverSprite;
            coverImg.preserveAspect = true;
        }
        else
        {
            // Фоллбек — цветная заливка
            Color c = accentColor;
            if (!string.IsNullOrEmpty(meta.coverColor))
                ColorUtility.TryParseHtmlString(meta.coverColor, out c);
            coverImg.color = c;
        }
        var coverLE = coverGO.AddComponent<LayoutElement>();
        coverLE.minWidth = coverLE.preferredWidth = 120;

        // ── Текстовый блок ───────────────────────────────────────────────────
        var info = new GameObject("Info");
        info.transform.SetParent(card.transform, false);
        info.AddComponent<RectTransform>();
        var infoVLG = info.AddComponent<VerticalLayoutGroup>();
        infoVLG.spacing = 8;
        infoVLG.childForceExpandWidth = true;
        infoVLG.childForceExpandHeight = false;
        infoVLG.childAlignment = TextAnchor.MiddleLeft;
        infoVLG.childControlWidth = true;
        infoVLG.childControlHeight = true;
        var infoLE = info.AddComponent<LayoutElement>();
        infoLE.flexibleWidth = 1;

        MakeText("Title", info.transform, meta.title, 36, textColor, TextAlignmentOptions.Left);
        MakeText("Author", info.transform, meta.author ?? "", 28, subtextColor, TextAlignmentOptions.Left);

        // Кнопка «Оқу»
        var btnGO = new GameObject("ReadBtn");
        btnGO.transform.SetParent(info.transform, false);
        var btnImg = btnGO.AddComponent<Image>();
        btnImg.color = accentColor;
        var btnLE = btnGO.AddComponent<LayoutElement>();
        btnLE.preferredHeight = 48;
        btnLE.preferredWidth = 160;

        MakeText("BtnTxt", btnGO.transform, "Оқу", 32, Color.white, TextAlignmentOptions.Center);
        SetRect(btnGO.transform.GetChild(0).gameObject, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        var btn = btnGO.AddComponent<Button>();
        btn.targetGraphic = btnImg;
        string fn = meta.fileName;
        btn.onClick.AddListener(() => OpenBook(fn));
    }

    // ═════════════════════════════════════════════════════════════════════════
    //  ЧТЕНИЕ КНИГИ
    // ═════════════════════════════════════════════════════════════════════════
    void OpenBook(string fileName)
    {
        var asset = Resources.Load<TextAsset>("Books/" + fileName);
        if (asset == null) { Debug.LogError("[Reader] Файл не найден: " + fileName); return; }
        var data = JsonUtility.FromJson<BookData>(asset.text);
        if (data == null || string.IsNullOrEmpty(data.content)) return;

        pages = SplitIntoPages(data.content, data.charsPerPage > 0 ? data.charsPerPage : 600);
        currentPage = 0;

        if (readerScreen == null) BuildReaderScreen();
        libraryScreen.SetActive(false);
        readerScreen.SetActive(true);
        readerTitleText.text = data.title;
        ShowPage(0);
    }

    void BuildReaderScreen()
    {
        readerScreen = MakePanel("ReaderScreen", mainCanvas.transform, readerBg);

        // ── Верхняя панель ───────────────────────────────────────────────────
        var topBar = MakePanel("TopBar", readerScreen.transform, new Color(0.92f, 0.90f, 0.85f));
        SetRect(topBar, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -120), new Vector2(0, 0));

        // «← Артқа»
        var backGO = new GameObject("BackBtn");
        backGO.transform.SetParent(topBar.transform, false);
        var backImg = backGO.AddComponent<Image>();
        backImg.color = backBtnColor;
        SetRect(backGO, new Vector2(0, 0.5f), new Vector2(0, 0.5f),
                new Vector2(15, -25), new Vector2(135, 25));
        MakeText("Txt", backGO.transform, "← Артқа", 32, Color.white, TextAlignmentOptions.Center);
        SetRect(backGO.transform.GetChild(0).gameObject, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        var backBtn = backGO.AddComponent<Button>();
        backBtn.targetGraphic = backImg;
        backBtn.onClick.AddListener(() => { readerScreen.SetActive(false); libraryScreen.SetActive(true); });

        // Название книги
        readerTitleText = MakeText("BookTitle", topBar.transform, "", 40, readerText, TextAlignmentOptions.Center);
        SetRect(readerTitleText.gameObject, new Vector2(0.15f, 0), new Vector2(0.85f, 1),
                new Vector2(0, 5), new Vector2(0, -5));

        // ── Текст ────────────────────────────────────────────────────────────
        readerPageText = MakeText("PageText", readerScreen.transform, "", 36, readerText, TextAlignmentOptions.TopLeft);
        readerPageText.lineSpacing = 14;
        readerPageText.overflowMode = TextOverflowModes.Truncate;
        SetRect(readerPageText.gameObject, new Vector2(0, 0.07f), new Vector2(1, 1),
                new Vector2(45, 10), new Vector2(-45, -135));

        // ── Нижняя панель ────────────────────────────────────────────────────
        var bottomBar = MakePanel("BottomBar", readerScreen.transform, new Color(0.92f, 0.90f, 0.85f));
        SetRect(bottomBar, Vector2.zero, new Vector2(1, 0), Vector2.zero, new Vector2(0, 100));

        readerPrevBtn = MakeNavButton("PrevBtn", bottomBar.transform, "← Алдыңғы",
            new Vector2(0, 0), new Vector2(0.33f, 1), new Vector2(10, 12), new Vector2(-5, -12));
        readerPrevBtn.onClick.AddListener(() => { if (currentPage > 0) ShowPage(currentPage - 1); });

        readerCounterText = MakeText("Counter", bottomBar.transform, "1/1", 32, readerText, TextAlignmentOptions.Center);
        SetRect(readerCounterText.gameObject, new Vector2(0.33f, 0), new Vector2(0.66f, 1),
                new Vector2(5, 12), new Vector2(-5, -12));

        readerNextBtn = MakeNavButton("NextBtn", bottomBar.transform, "Келесі →",
            new Vector2(0.66f, 0), new Vector2(1, 1), new Vector2(5, 12), new Vector2(-10, -12));
        readerNextBtn.onClick.AddListener(() => { if (currentPage < pages.Count - 1) ShowPage(currentPage + 1); });
    }

    void ShowPage(int idx)
    {
        idx = Mathf.Clamp(idx, 0, pages.Count - 1);
        currentPage = idx;
        readerPageText.text = pages[idx];
        readerCounterText.text = $"{idx + 1} / {pages.Count}";
        readerPrevBtn.interactable = idx > 0;
        readerNextBtn.interactable = idx < pages.Count - 1;
    }

    // ═════════════════════════════════════════════════════════════════════════
    //  РАЗБИВКА НА СТРАНИЦЫ
    // ═════════════════════════════════════════════════════════════════════════
    List<string> SplitIntoPages(string text, int maxChars)
    {
        var result = new List<string>();
        text = text.Replace("\\n", "\n").Replace("\r\n", "\n").Replace("\r", "\n");
        string[] paragraphs = text.Split(new[] { "\n\n" }, System.StringSplitOptions.RemoveEmptyEntries);
        var sb = new System.Text.StringBuilder();

        foreach (string para in paragraphs)
        {
            string p = para.Trim();
            if (string.IsNullOrEmpty(p)) continue;
            string sep = sb.Length > 0 ? "\n\n" : "";
            if (sb.Length + sep.Length + p.Length <= maxChars)
            {
                sb.Append(sep).Append(p);
            }
            else
            {
                foreach (string word in p.Split(' '))
                {
                    string ws = sb.Length > 0 ? " " : "";
                    if (sb.Length + ws.Length + word.Length > maxChars)
                    {
                        if (sb.Length > 0) { result.Add(sb.ToString().Trim()); sb.Clear(); }
                        sb.Append(word);
                    }
                    else sb.Append(ws).Append(word);
                }
                if (sb.Length + 2 <= maxChars) sb.Append("\n");
            }
        }
        if (sb.Length > 0) result.Add(sb.ToString().Trim());
        if (result.Count == 0) result.Add("");
        return result;
    }

    // ═════════════════════════════════════════════════════════════════════════
    //  УТИЛИТЫ
    // ═════════════════════════════════════════════════════════════════════════
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

    Button MakeNavButton(string name, Transform parent, string label,
        Vector2 aMin, Vector2 aMax, Vector2 oMin, Vector2 oMax)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = accentColor;
        SetRect(go, aMin, aMax, oMin, oMax);
        var tmp = MakeText(name + "Txt", go.transform, label, 32, Color.white, TextAlignmentOptions.Center);
        SetRect(tmp.gameObject, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        return btn;
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
}
