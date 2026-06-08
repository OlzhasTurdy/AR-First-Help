using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class CPRPracticeController : MonoBehaviour
{
    private TextMeshProUGUI feedbackText;
    private TextMeshProUGUI counterText;
    private TextMeshProUGUI resultText;

    [Header("Settings")]
    public int targetCompressions = 30;
    public float minInterval = 0.5f;
    public float maxInterval = 0.7f;

    [Header("Hit Target")]
    public Transform compressionTarget;
    public string compressionTargetTag = "CPRTarget";
    public string[] preferredTargetNameParts = { "CPRTarget", "RedZone", "Red", "Chest", "Grud" };
    public string[] fallbackTargetNameParts = { "CPRTarget", "RedZone", "Red", "Chest", "Grud" };
    public LayerMask raycastLayers = ~0;
    public float raycastDistance = 100f;

    [Header("Compression Visuals")]
    public Transform chestTransform;
    public float compressionScale = 0.9f;
    public float compressionDuration = 0.12f;

    [Header("Simple Hand Motion")]
    public bool useSimpleHandMotion = true;
    public Transform movingHandTransform;
    public float handDownLocalY = 1.44f;
    public float handUpLocalY = 2.6f;
    public float handMoveDuration = 0.16f;

    private int compressionCount = 0;
    private float lastPressTime = 0f;
    private float totalIntervalTime = 0f;
    private bool practiceActive = false;
    private Vector3 originalChestScale;
    private Coroutine chestAnimationRoutine;
    private Coroutine handMotionRoutine;

    public void Initialize(
        TextMeshProUGUI feedback,
        TextMeshProUGUI counter,
        TextMeshProUGUI result)
    {
        feedbackText = feedback;
        counterText = counter;
        resultText = result;
    }

    public void StartPractice()
    {
        AutoBindPrefabReferences();

        compressionCount = 0;
        lastPressTime = 0f;
        totalIntervalTime = 0f;
        practiceActive = true;

        if (chestTransform != null)
            originalChestScale = chestTransform.localScale;

        if (movingHandTransform != null)
        {
            movingHandTransform.gameObject.SetActive(true);
            SetHandLocalY(handUpLocalY);
        }

        if (counterText) counterText.text = "Count: 0";
        if (feedbackText) feedbackText.text = "Start Compressions!";
        if (resultText) resultText.text = "";
    }

    void Update()
    {
        if (!practiceActive) return;
        if (!TryGetPressPosition(out Vector2 pressPosition, out int pointerId)) return;

        if (EventSystem.current != null &&
            pointerId >= 0 &&
            EventSystem.current.IsPointerOverGameObject(pointerId))
            return;

        Camera mainCamera = Camera.main;
        if (mainCamera == null) return;

        Ray ray = mainCamera.ScreenPointToRay(pressPosition);
        if (!TryFindCompressionHit(ray, out _)) return;

        RegisterCompression();
        PlayCompressionVisuals();
    }

    bool TryGetPressPosition(out Vector2 position, out int pointerId)
    {
        pointerId = -1;

#if UNITY_EDITOR
        if (Input.GetMouseButtonDown(0))
        {
            position = Input.mousePosition;
            return true;
        }
#endif

        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            pointerId = touch.fingerId;

            if (touch.phase == TouchPhase.Began)
            {
                position = touch.position;
                return true;
            }
        }

        position = default;
        return false;
    }

    bool TryFindCompressionHit(Ray ray, out RaycastHit targetHit)
    {
        RaycastHit[] hits = Physics.RaycastAll(ray, raycastDistance, raycastLayers, QueryTriggerInteraction.Collide);
        if (hits.Length == 0)
        {
            targetHit = default;
            return false;
        }

        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (RaycastHit hit in hits)
        {
            if (IsCompressionTarget(hit.collider))
            {
                targetHit = hit;
                return true;
            }
        }

        targetHit = default;
        return false;
    }

    bool IsCompressionTarget(Collider hitCollider)
    {
        if (hitCollider == null) return false;

        Transform hitTransform = hitCollider.transform;
        if (compressionTarget != null &&
            (hitTransform == compressionTarget || hitTransform.IsChildOf(compressionTarget)))
            return true;

        if (!string.IsNullOrEmpty(compressionTargetTag) &&
            HasTag(hitCollider.gameObject, compressionTargetTag))
            return true;

        foreach (string namePart in fallbackTargetNameParts)
        {
            if (string.IsNullOrEmpty(namePart)) continue;
            if (hitTransform.name.IndexOf(namePart, System.StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
        }

        return false;
    }

    bool HasTag(GameObject target, string tagName)
    {
        try
        {
            return target.CompareTag(tagName);
        }
        catch (UnityException)
        {
            return false;
        }
    }

    void RegisterCompression()
    {
        float currentTime = Time.time;

        if (lastPressTime > 0f)
        {
            float interval = currentTime - lastPressTime;
            totalIntervalTime += interval;

            if (feedbackText)
            {
                if (interval < minInterval)
                    feedbackText.text = "Slower!";
                else if (interval > maxInterval)
                    feedbackText.text = "Faster!";
                else
                    feedbackText.text = "Good Rhythm";
            }
        }

        lastPressTime = currentTime;
        compressionCount++;

        if (counterText) counterText.text = "Count: " + compressionCount;

        if (compressionCount >= targetCompressions)
            FinishPractice();
    }

    void PlayCompressionVisuals()
    {
        if (useSimpleHandMotion && movingHandTransform != null)
        {
            if (handMotionRoutine != null)
                StopCoroutine(handMotionRoutine);

            handMotionRoutine = StartCoroutine(AnimateSimpleHandMotion());
        }
        if (chestTransform != null)
        {
            if (chestAnimationRoutine != null)
                StopCoroutine(chestAnimationRoutine);

            chestAnimationRoutine = StartCoroutine(AnimateChestCompression());
        }
    }

    IEnumerator AnimateSimpleHandMotion()
    {
        yield return MoveHandToLocalY(handDownLocalY, handMoveDuration * 0.5f);
        yield return MoveHandToLocalY(handUpLocalY, handMoveDuration * 0.5f);
    }

    IEnumerator MoveHandToLocalY(float targetY, float duration)
    {
        if (movingHandTransform == null) yield break;

        Vector3 startPosition = movingHandTransform.localPosition;
        Vector3 targetPosition = startPosition;
        targetPosition.y = targetY;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = duration > 0f ? Mathf.Clamp01(elapsed / duration) : 1f;
            movingHandTransform.localPosition = Vector3.Lerp(startPosition, targetPosition, t);
            yield return null;
        }

        movingHandTransform.localPosition = targetPosition;
    }

    void SetHandLocalY(float targetY)
    {
        if (movingHandTransform == null) return;

        Vector3 position = movingHandTransform.localPosition;
        position.y = targetY;
        movingHandTransform.localPosition = position;
    }

    IEnumerator AnimateChestCompression()
    {
        chestTransform.localScale = originalChestScale * compressionScale;
        yield return new WaitForSeconds(compressionDuration);

        if (chestTransform != null)
            chestTransform.localScale = originalChestScale;
    }

    void AutoBindPrefabReferences()
    {
        if (compressionTarget == null)
            compressionTarget = FindChildByNames(transform, preferredTargetNameParts);

        if (compressionTarget == null)
            compressionTarget = FindChildByNames(transform, fallbackTargetNameParts);

        EnsureCompressionTargetCollider();

        if (chestTransform == null)
            chestTransform = compressionTarget != null
                ? compressionTarget
                : FindChildByNames(transform, new string[] { "Chest", "Grud" });

    }

    void EnsureCompressionTargetCollider()
    {
        if (compressionTarget == null) return;

        BoxCollider collider = compressionTarget.GetComponent<BoxCollider>();
        if (collider == null)
            collider = compressionTarget.gameObject.AddComponent<BoxCollider>();

        collider.isTrigger = true;
        FitColliderToRenderers(collider, compressionTarget);
    }

    void FitColliderToRenderers(BoxCollider collider, Transform root)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
        {
            collider.center = Vector3.zero;
            collider.size = Vector3.one;
            return;
        }

        bool hasBounds = false;
        Bounds localBounds = new Bounds(Vector3.zero, Vector3.zero);

        foreach (Renderer renderer in renderers)
        {
            Bounds worldBounds = renderer.bounds;
            Vector3 worldMin = worldBounds.min;
            Vector3 worldMax = worldBounds.max;

            Vector3[] corners =
            {
                new Vector3(worldMin.x, worldMin.y, worldMin.z),
                new Vector3(worldMin.x, worldMin.y, worldMax.z),
                new Vector3(worldMin.x, worldMax.y, worldMin.z),
                new Vector3(worldMin.x, worldMax.y, worldMax.z),
                new Vector3(worldMax.x, worldMin.y, worldMin.z),
                new Vector3(worldMax.x, worldMin.y, worldMax.z),
                new Vector3(worldMax.x, worldMax.y, worldMin.z),
                new Vector3(worldMax.x, worldMax.y, worldMax.z)
            };

            foreach (Vector3 corner in corners)
            {
                Vector3 localPoint = root.InverseTransformPoint(corner);
                if (!hasBounds)
                {
                    localBounds = new Bounds(localPoint, Vector3.zero);
                    hasBounds = true;
                }
                else
                {
                    localBounds.Encapsulate(localPoint);
                }
            }
        }

        collider.center = localBounds.center;
        collider.size = new Vector3(
            Mathf.Max(localBounds.size.x, 0.01f),
            Mathf.Max(localBounds.size.y, 0.01f),
            Mathf.Max(localBounds.size.z, 0.01f));
    }

    Transform FindChildByNames(Transform root, IEnumerable<string> nameParts)
    {
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
        {
            foreach (string namePart in nameParts)
            {
                if (string.IsNullOrEmpty(namePart)) continue;
                if (child.name.IndexOf(namePart, System.StringComparison.OrdinalIgnoreCase) >= 0)
                    return child;
            }
        }

        return null;
    }

    void FinishPractice()
    {
        practiceActive = false;

        float averageInterval = compressionCount > 1
            ? totalIntervalTime / (compressionCount - 1)
            : maxInterval;

        float bpm = 60f / Mathf.Max(averageInterval, 0.01f);
        string grade;

        if (bpm >= 100f && bpm <= 120f)
            grade = "Excellent";
        else if (bpm >= 90f && bpm <= 130f)
            grade = "Good";
        else
            grade = "Needs Improvement";

        if (resultText)
        {
            resultText.text =
                "Practice Completed!\n" +
                "Average BPM: " + bpm.ToString("F0") +
                "\nResult: " + grade;
        }
    }
}
