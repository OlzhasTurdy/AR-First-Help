using System.Collections;
using UnityEngine;
using TMPro;

public class TypewriterEffect : MonoBehaviour
{
    public TextMeshProUGUI textComponent; // Мәтін компоненті
    public float typingSpeed = 0.05f;    // Әріптердің шығу жылдамдығы

    private string fullText;
    private string currentText = "";

    void Start()
    {
        // Мәтінді басында сақтап алып, экранды тазалаймыз
        fullText = textComponent.text;
        textComponent.text = "";

        // Эффектіні бастау
        StartCoroutine(ShowText());
    }

    IEnumerator ShowText()
    {
        for (int i = 0; i <= fullText.Length; i++)
        {
            currentText = fullText.Substring(0, i);
            textComponent.text = currentText;

            // Әр әріптен кейін азғантай кідіріс
            yield return new WaitForSeconds(typingSpeed);
        }
    }
}