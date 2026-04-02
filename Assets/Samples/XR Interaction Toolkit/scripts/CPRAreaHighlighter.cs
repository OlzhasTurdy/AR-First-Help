using UnityEngine;

public class CPRAreaHighlighter : MonoBehaviour
{
    public Transform leftShoulder;
    public Transform rightShoulder;
    public Transform leftHip;
    public Transform rightHip;

    public GameObject redZoneIndicator; // Префаб красного круга/квадрата

    void Update()
    {
        if (leftShoulder == null || rightShoulder == null) return;

        // 1. Находим центр плечевого пояса
        Vector3 shoulderCenter = (leftShoulder.position + rightShoulder.position) / 2f;

        // 2. Находим центр таза
        Vector3 hipCenter = (leftHip.position + rightHip.position) / 2f;

        // 3. Точка для СЛР (примерно нижняя треть грудины)
        // Смещаемся от плеч вниз на 20-30% расстояния до бедер
        Vector3 cprPoint = Vector3.Lerp(shoulderCenter, hipCenter, 0.25f);

        // 4. Устанавливаем позицию подсветки
        redZoneIndicator.transform.position = cprPoint;

        // 5. Опционально: поворачиваем плоскость параллельно телу
        Vector3 chestNormal = Vector3.Cross(rightShoulder.position - leftShoulder.position, hipCenter - shoulderCenter);
        redZoneIndicator.transform.rotation = Quaternion.LookRotation(chestNormal);
    }
}