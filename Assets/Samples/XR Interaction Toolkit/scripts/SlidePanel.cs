using UnityEngine;
using System.Collections;

public class SlidePanel : MonoBehaviour
{
    public RectTransform panel;

    public float loginY = 1316f;
    public float registerY = -570f;

    public float duration = 0.4f;

    private bool isRegister = false;
    private Coroutine currentAnimation;

    public void TogglePanel()
    {
        if (currentAnimation != null)
            StopCoroutine(currentAnimation);

        float targetY = isRegister ? loginY : registerY;
        Debug.Log("Toggle clicked");

        currentAnimation = StartCoroutine(SmoothMove(targetY));

        isRegister = !isRegister;
    }

    IEnumerator SmoothMove(float targetY)
    {
        Vector2 startPos = panel.anchoredPosition;
        Vector2 targetPos = new Vector2(startPos.x, targetY);

        float time = 0f;

        while (time < duration)
        {
            time += Time.deltaTime;
            float t = time / duration;

            // плавность ease-in-out
            t = Mathf.SmoothStep(0, 1, t);

            panel.anchoredPosition = Vector2.Lerp(startPos, targetPos, t);

            yield return null;
        }

        panel.anchoredPosition = targetPos;
    }
}
