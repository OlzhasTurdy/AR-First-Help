using Mediapipe.Unity; // Убедись, что это пространство имен подключено
using UnityEngine;
using Mediapipe.Unity.Sample;
public class ARToMediaPipeBridge : MonoBehaviour
{
    public void SetupAR()
    {
        // Мы не используем переменную, а сразу берем данные из "облака" (статического класса)
        var currentSource = ImageSourceProvider.ImageSource;

        if (currentSource != null)
        {
            // Вместо currentSource.name используй это:
            Debug.Log("Работаем с источником типа: " + currentSource.GetType().Name);
        }
    }
}