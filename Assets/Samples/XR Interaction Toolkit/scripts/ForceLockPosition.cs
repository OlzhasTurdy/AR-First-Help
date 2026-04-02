using UnityEngine;

public class ForceLockPosition : MonoBehaviour
{
    public Transform rescuer;
    public Transform victim;

    // Ваши проверенные координаты
    private Vector3 rescuerPos = new Vector3(0.3f, 0f, 0.27f);
    private Vector3 rescuerRot = new Vector3(0f, 100f, 0f);

    void Start()
    {
        // Если ссылки не назначены в инспекторе, ищем их по имени в дочерних объектах
        if (rescuer == null) rescuer = transform.Find("X Bot@Administering Cpr");
        if (victim == null) victim = transform.Find("X Bot@Receiving Cpr");

        // Важно: на скрине image_c87e1b объект называется "X Bot@Receiving Cpr"
        // Убедитесь, что имя в кавычках СТРОГО совпадает с именем в Hierarchy
    }

    void LateUpdate()
    {
        if (victim != null)
        {
            victim.localPosition = Vector3.zero;
            victim.localRotation = Quaternion.identity;
        }

        if (rescuer != null)
        {
            rescuer.localPosition = rescuerPos;
            rescuer.localEulerAngles = rescuerRot;
        }
    }
}