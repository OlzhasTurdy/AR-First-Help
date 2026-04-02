using UnityEngine;

public class ARObjectManipulator : MonoBehaviour
{
    private Vector3 offset;
    private float initialDistance;
    private Vector3 initialScale;

    void Update()
    {
        if (Input.touchCount == 1)
        {
            Touch touch = Input.GetTouch(0);

            if (touch.phase == TouchPhase.Began)
            {
                Ray ray = Camera.main.ScreenPointToRay(touch.position);
                RaycastHit hit;

                if (Physics.Raycast(ray, out hit))
                {
                    if (hit.transform == transform)
                    {
                        offset = transform.position - hit.point;
                    }
                }
            }

            if (touch.phase == TouchPhase.Moved)
            {
                Ray ray = Camera.main.ScreenPointToRay(touch.position);
                Plane plane = new Plane(Vector3.up, transform.position);
                float distance;

                if (plane.Raycast(ray, out distance))
                {
                    Vector3 point = ray.GetPoint(distance);
                    transform.position = point + offset;
                }
            }
        }

        if (Input.touchCount == 2)
        {
            Touch t1 = Input.GetTouch(0);
            Touch t2 = Input.GetTouch(1);

            float currentDistance = Vector2.Distance(t1.position, t2.position);

            if (t2.phase == TouchPhase.Began)
            {
                initialDistance = currentDistance;
                initialScale = transform.localScale;
            }
            else
            {
                float scaleFactor = currentDistance / initialDistance;
                transform.localScale = initialScale * scaleFactor;
            }
        }
    }
}
