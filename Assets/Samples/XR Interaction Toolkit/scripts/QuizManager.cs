using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class Question
{
    public string questionText;
    public string[] answers;
    public int correctIndex;
}

public class QuizManager : MonoBehaviour
{
    public TextMeshProUGUI questionText;
    public Button[] answerButtons;
    public TextMeshProUGUI progressText;
    public Image progressFill;

    public List<Question> questions;

    private int currentQuestion = 0;
    private int score = 0;

    void Start()
    {
        LoadQuestion();
    }

    void LoadQuestion()
    {
        Question q = questions[currentQuestion];

        questionText.text = q.questionText;
        progressText.text = (currentQuestion + 1) + " / " + questions.Count;
        progressFill.fillAmount = (float)(currentQuestion + 1) / questions.Count;

        for (int i = 0; i < answerButtons.Length; i++)
        {
            int index = i;
            answerButtons[i].GetComponentInChildren<TextMeshProUGUI>().text = q.answers[i];
            answerButtons[i].onClick.RemoveAllListeners();
            answerButtons[i].onClick.AddListener(() => CheckAnswer(index));
            answerButtons[i].image.color = Color.white;
        }
    }

    void CheckAnswer(int index)
    {
        Question q = questions[currentQuestion];

        if (index == q.correctIndex)
        {
            score++;
            answerButtons[index].image.color = Color.green;
        }
        else
        {
            answerButtons[index].image.color = Color.red;
            answerButtons[q.correctIndex].image.color = Color.green;
        }

        StartCoroutine(NextQuestion());
    }

    IEnumerator NextQuestion()
    {
        yield return new WaitForSeconds(1f);

        currentQuestion++;

        if (currentQuestion < questions.Count)
            LoadQuestion();
        else
            Debug.Log("Finished! Score: " + score);
    }
}
