using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

/// <summary>
/// LibraryController — упрощённая версия для ручной расстановки кнопок.
///
/// В Инспекторе заполните массив bookSlots:
///   каждый слот = одна кнопка в сцене + её TMP-текст.
/// Книги из books_catalog.json назначаются по порядку слотов.
/// </summary>
public class LibraryController : MonoBehaviour
{
    [Header("Кнопки книг (ручные — расставлены в сцене)")]
    public BookSlot[] bookSlots;   // Заполните в Inspector, до 6 штук

    [Header("Кнопка назад")]
    public Button backButton;

    [Header("Навигация")]
    public string bookSceneName = "Theoryroom";
    public string backSceneName = "MainMenu";

    // ── Слот: одна кнопка + её TMP ───────────────────────────────────────────
    [System.Serializable]
    public class BookSlot
    {
        public Button          button;       // Кнопка в сцене
        public TextMeshProUGUI titleText;    // Текст названия на кнопке
        public TextMeshProUGUI authorText;   // Текст автора (опционально)
        public Image           coverImage;   // Image кнопки для покраски (опционально)
    }

    // ── Модели JSON ──────────────────────────────────────────────────────────
    [System.Serializable]
    private class BookMeta
    {
        public string fileName;
        public string title;
        public string author;
        public string coverColor;
    }

    [System.Serializable]
    private class BookCatalog
    {
        public List<BookMeta> books;
    }

    // ────────────────────────────────────────────────────────────────────────
    void Start()
    {
        if (backButton != null)
            backButton.onClick.AddListener(() => SceneManager.LoadScene(backSceneName));

        // Сначала скрываем все слоты
        foreach (var slot in bookSlots)
            if (slot.button != null)
                slot.button.gameObject.SetActive(false);

        // Загружаем каталог
        TextAsset asset = Resources.Load<TextAsset>("Books/books_catalog");
        if (asset == null)
        {
            Debug.LogError("[LibraryController] books_catalog.json не найден в Resources/Books/");
            return;
        }

        BookCatalog catalog = JsonUtility.FromJson<BookCatalog>(asset.text);
        if (catalog == null || catalog.books == null || catalog.books.Count == 0)
        {
            Debug.LogWarning("[LibraryController] Каталог пустой.");
            return;
        }

        // Назначаем книги кнопкам по порядку
        int count = Mathf.Min(bookSlots.Length, catalog.books.Count);
        for (int i = 0; i < count; i++)
        {
            BookSlot  slot = bookSlots[i];
            BookMeta  meta = catalog.books[i];

            if (slot.button == null) continue;

            // Показываем кнопку
            slot.button.gameObject.SetActive(true);

            // Только текст — цвет и изображения НЕ трогаем (расставлены вручную)
            if (slot.titleText != null)
                slot.titleText.text = meta.title;

            if (slot.authorText != null)
                slot.authorText.text = meta.author;

            // Клик → открыть книгу
            string fileName = meta.fileName;   // захватываем для лямбды
            slot.button.onClick.RemoveAllListeners();
            slot.button.onClick.AddListener(() => OpenBook(fileName));
        }

        Debug.Log($"[LibraryController] Назначено книг: {count}");
    }

    private void OpenBook(string fileName)
    {
        PlayerPrefs.SetString("SelectedBookFile", fileName);
        PlayerPrefs.Save();
        SceneManager.LoadScene(bookSceneName);
    }
}
