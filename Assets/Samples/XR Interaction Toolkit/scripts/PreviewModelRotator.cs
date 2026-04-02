using UnityEngine;

public class PreviewModelRotator : MonoBehaviour
{
    [Header("Rotation")]
    public float rotationSpeed = 200f;

    [Header("Zoom")]
    public float zoomSensitivity = 0.1f;
    public float minScale = 0.05f;
    public float maxScale = 0.5f;

    private float currentScale;

    void Start()
    {
        currentScale = transform.localScale.x;
    }

    void Update()
    {
        HandleRotation();
        HandleZoom();
    }

    void HandleRotation()
    {
#if UNITY_EDITOR
        if (Input.GetMouseButton(0))
        {
            float mouseX = Input.GetAxis("Mouse X");
            transform.Rotate(Vector3.up, -mouseX * rotationSpeed * Time.deltaTime, Space.World);
        }
#endif

        if (Input.touchCount == 1)
        {
            Touch touch = Input.GetTouch(0);

            if (touch.phase == TouchPhase.Moved)
            {
                transform.Rotate(Vector3.up, -touch.deltaPosition.x * rotationSpeed * Time.deltaTime, Space.World);
            }
        }
    }

    void HandleZoom()
    {
#if UNITY_EDITOR
        float scroll = Input.GetAxis("Mouse ScrollWheel");

        if (Mathf.Abs(scroll) > 0.001f)
        {
            currentScale *= (1 + scroll * zoomSensitivity);
            currentScale = Mathf.Clamp(currentScale, minScale, maxScale);
            transform.localScale = Vector3.one * currentScale;
        }
#endif

        if (Input.touchCount == 2)
        {
            Touch touch1 = Input.GetTouch(0);
            Touch touch2 = Input.GetTouch(1);

            Vector2 prevPos1 = touch1.position - touch1.deltaPosition;
            Vector2 prevPos2 = touch2.position - touch2.deltaPosition;

            float prevDistance = Vector2.Distance(prevPos1, prevPos2);
            float currentDistance = Vector2.Distance(touch1.position, touch2.position);

            float delta = currentDistance - prevDistance;

            currentScale *= (1 + delta * 0.001f);
            currentScale = Mathf.Clamp(currentScale, minScale, maxScale);
            transform.localScale = Vector3.one * currentScale;
        }
    }
}
