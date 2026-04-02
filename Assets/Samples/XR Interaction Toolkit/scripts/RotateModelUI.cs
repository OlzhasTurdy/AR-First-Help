using UnityEngine;
using UnityEngine.EventSystems;

// Скрипт вешается на RawImage в UI
public class RotateModelUI : MonoBehaviour, IDragHandler
{
    [Header("Объект для вращения (Модель)")]
    public Transform modelToRotate;

    [Header("Настройки чувствительности")]
    public float sensitivity = 0.4f;

    // Метод срабатывает при каждом движении пальца/мыши по RawImage
    public void OnDrag(PointerEventData eventData)
    {
        if (modelToRotate != null)
        {
            // 1. Получаем смещение пальца
            float deltaX = eventData.delta.x * sensitivity;
            float deltaY = eventData.delta.y * sensitivity;

            // 2. Вращаем вокруг оси Y (влево-вправо)
            // Используем Space.World, чтобы вращение всегда было "земным"
            modelToRotate.Rotate(Vector3.up, -deltaX, Space.World);

            // 3. Вращаем вокруг оси X (вверх-вниз)
            // Используем Space.Self или Vector3.right для наклона модели на пользователя/от него
            modelToRotate.Rotate(Vector3.right, deltaY, Space.World);
        }
    }
}