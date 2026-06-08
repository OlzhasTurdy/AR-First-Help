using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

/// <summary>
/// BookController — загружает JSON-книгу из Resources/Books/
/// и автоматически разбивает текст на страницы.
///
/// Привяжите в Инспекторе:
///   bookFileName    — файл по умолчанию (если не открыта из библиотеки)
///   titleText       — TMP для заголовка/автора
///   pageText        — TMP для текста страницы  (MainText)
///   pageCounterText — TMP для "1 / 12"         (NumericPagesTMP)
///   prevButton      — кнопка «Алдыңғы»
///   nextButton      — кнопка «Келесі Бет»
///   backButton      — кнопка «←» (вернуться в библиотеку)
/// </summary>
public class BookController : MonoBehaviour
{
    [Header("Файл книги по умолчанию (Resources/Books/)")]
    public string bookFileName = "book_first_aid";

    [Header("UI")]
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI pageText;
    public TextMeshProUGUI pageCounterText;
    public Button          prevButton;
    public Button          nextButton;
    public Button          backButton;

    [Header("Навигация")]
    public string librarySceneName = "library";   // Имя сцены библиотеки

    // ── Внутреннее состояние ─────────────────────────────────────────────────
    private List<string> pages      = new List<string>();
    private int          currentPage = 0;   // 0-based

    // ── Модель JSON ──────────────────────────────────────────────────────────
    [System.Serializable]
    private class BookData
    {
        public string title;
        public string author;
        public int    charsPerPage = 600;   // символов на страницу (по умолчанию)
        public string content;
    }

    // ────────────────────────────────────────────────────────────────────────
    void Start()
    {
        if (prevButton != null) prevButton.onClick.AddListener(PrevPage);
        if (nextButton != null) nextButton.onClick.AddListener(NextPage);
        if (backButton != null) backButton.onClick.AddListener(GoToLibrary);

        // Если открыта из библиотеки — берём имя файла из PlayerPrefs
        string selected = PlayerPrefs.GetString("SelectedBookFile", "");
        string fileToLoad = string.IsNullOrEmpty(selected) ? bookFileName : selected;

        LoadBook(fileToLoad);
    }

    // ── Возврат в библиотеку ─────────────────────────────────────────────────
    public void GoToLibrary()
    {
        PlayerPrefs.DeleteKey("SelectedBookFile");
        PlayerPrefs.Save();
        SceneManager.LoadScene(librarySceneName);
    }

    // ── Загрузка и разбивка ──────────────────────────────────────────────────
    public void LoadBook(string fileName)
    {
        // Загружаем JSON из Assets/Resources/Books/<fileName>.json
        TextAsset asset = Resources.Load<TextAsset>("Books/" + fileName);
        if (asset == null)
        {
            Debug.LogError("[BookController] Файл не найден: Resources/Books/" + fileName + ".json");
            return;
        }

        BookData data = JsonUtility.FromJson<BookData>(asset.text);
        if (data == null || string.IsNullOrEmpty(data.content))
        {
            Debug.LogError("[BookController] Некорректный JSON или пустой контент.");
            return;
        }

        // Заголовок
        if (titleText != null)
            titleText.text = data.title + (string.IsNullOrEmpty(data.author)
                                           ? ""
                                           : "\n<size=70%><color=#888888>" + data.author + "</color></size>");

        // Разбиваем на страницы
        int charsPerPage = data.charsPerPage > 0 ? data.charsPerPage : 600;
        pages = SplitIntoPages(data.content, charsPerPage);

        currentPage = 0;
        ShowPage(currentPage);

        Debug.Log($"[BookController] Книга загружена: {pages.Count} страниц");
    }

    // ── Алгоритм разбивки ────────────────────────────────────────────────────
    /// <summary>
    /// Разбивает сплошной текст на страницы по словам,
    /// не превышая <paramref name="maxChars"/> символов на страницу.
    /// Абзацы (двойной перенос \n\n) всегда начинаются с новой строки,
    /// но НЕ обязательно с новой страницы.
    /// </summary>
    private static List<string> SplitIntoPages(string text, int maxChars)
    {
        var result = new List<string>();

        // Нормализуем переносы строк
        text = text.Replace("\r\n", "\n").Replace("\r", "\n");

        // Делим на «блоки» (абзацы разделены \n\n, одиночные \n сохраняем)
        string[] paragraphs = text.Split(new string[] { "\n\n" }, System.StringSplitOptions.RemoveEmptyEntries);

        System.Text.StringBuilder currentPageSB = new System.Text.StringBuilder();

        foreach (string paragraph in paragraphs)
        {
            string para = paragraph.Trim();
            if (string.IsNullOrEmpty(para)) continue;

            // Если абзац целиком влезает на текущую страницу — добавляем
            string separator = currentPageSB.Length > 0 ? "\n\n" : "";
            if (currentPageSB.Length + separator.Length + para.Length <= maxChars)
            {
                currentPageSB.Append(separator).Append(para);
            }
            else
            {
                // Не влезает — сначала проверим, можно ли начать абзац здесь частично
                // Разбиваем абзац на слова
                string[] words = para.Split(' ');
                foreach (string word in words)
                {
                    string w = word;
                    string sep = currentPageSB.Length > 0 ? " " : "";

                    if (currentPageSB.Length + sep.Length + w.Length > maxChars)
                    {
                        // Страница заполнена — сохраняем и начинаем новую
                        if (currentPageSB.Length > 0)
                        {
                            result.Add(currentPageSB.ToString().Trim());
                            currentPageSB.Clear();
                        }
                        currentPageSB.Append(w);
                    }
                    else
                    {
                        currentPageSB.Append(sep).Append(w);
                    }
                }

                // После абзаца добавляем двойной перенос (если влезет)
                if (currentPageSB.Length + 2 <= maxChars)
                    currentPageSB.Append("\n");
            }
        }

        // Последняя страница
        if (currentPageSB.Length > 0)
            result.Add(currentPageSB.ToString().Trim());

        // Если текст пустой — хотя бы одна пустая страница
        if (result.Count == 0)
            result.Add("");

        return result;
    }

    // ── Отображение страницы ─────────────────────────────────────────────────
    private void ShowPage(int index)
    {
        if (pages.Count == 0) return;

        // Гарантируем корректный диапазон
        index = Mathf.Clamp(index, 0, pages.Count - 1);
        currentPage = index;

        // Текст страницы
        if (pageText != null)
            pageText.text = pages[index];

        // Счётчик страниц
        if (pageCounterText != null)
            pageCounterText.text = $"{index + 1} / {pages.Count}";

        // Активность кнопок
        if (prevButton != null) prevButton.interactable = (index > 0);
        if (nextButton != null) nextButton.interactable = (index < pages.Count - 1);
    }

    // ── Навигация ────────────────────────────────────────────────────────────
    public void NextPage()
    {
        if (currentPage < pages.Count - 1)
            ShowPage(currentPage + 1);
    }

    public void PrevPage()
    {
        if (currentPage > 0)
            ShowPage(currentPage - 1);
    }

    /// <summary>Перейти к конкретной странице (1-based для удобства UI).</summary>
    public void GoToPage(int pageNumber)
    {
        ShowPage(pageNumber - 1);
    }

    // ── Публичные свойства ───────────────────────────────────────────────────
    public int CurrentPage  => currentPage + 1;         // 1-based
    public int TotalPages   => pages.Count;
}
