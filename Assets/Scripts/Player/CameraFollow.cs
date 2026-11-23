using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Smooth third-person orbit camera. Drag with mouse (Left, Right, or Middle click) or use Q/E
/// to rotate around the player. Mouse wheel zooms.
/// On level start, performs a cinematic fly-over of the whole course.
/// </summary>
public class CameraFollow : MonoBehaviour
{
    public Transform target;
    public float distance = 13.0f;
    public float minDistance = 7f;
    public float maxDistance = 24f;
    public float yaw = 0f;
    public float pitch = 40f;
    public float minPitch = 15f;
    public float maxPitch = 75f;
    public float keyboardRotateSpeed = 110f;
    public float mouseSensitivity = 0.22f;
    public float zoomSensitivity = 0.012f;
    public float trackpadYawSensitivity = 0.25f;
    public float followSmooth = 12f;
    public float rotationSmooth = 16f;
    public float targetHeight = 1.35f;
    public float lookAhead = 4.0f;

    // Fly-over settings
    [Header("Fly-Over")]
    public bool doFlyOver = true;
    public float flyOverHeight = 18f;
    public float flyOverDuration = 4.5f;
    public Transform gridRoot;             // assigned automatically via LevelManager

    private Vector3 smoothPivot;
    private bool initialized;
    private bool isDraggingMouse;

    // fly-over state
    private bool flyingOver;
    private bool flyOverDone;
    private float flyOverT;
    private Vector3 flyOverStart;
    private Vector3 flyOverEnd;

    public bool IsFlyOverDone => flyOverDone || !doFlyOver;

    void Start()
    {
        if (target != null)
        {
            smoothPivot = target.position + Vector3.up * targetHeight;
            initialized = true;
            SnapToTarget();
        }

        if (doFlyOver) StartCoroutine(FlyOverRoutine());
        else flyOverDone = true;
    }

    public IEnumerator FlyOverRoutine()
    {
        flyingOver = true;
        flyOverDone = false;
        flyOverT = 0f;

        // Determine bounding box of the course
        Bounds bounds = GetCourseBounds();
        Vector3 center = bounds.center;

        // Start: one end of the course. End: other end of the course.
        flyOverStart = new Vector3(bounds.min.x - 4f, center.y + flyOverHeight, center.z);
        flyOverEnd   = new Vector3(bounds.max.x + 4f, center.y + flyOverHeight, center.z);

        // Snap camera to start position immediately
        transform.position = flyOverStart;
        transform.LookAt(center);

        while (flyOverT < 1f)
        {
            flyOverT += Time.deltaTime / flyOverDuration;
            float t = Mathf.SmoothStep(0f, 1f, flyOverT);
            Vector3 pos = Vector3.Lerp(flyOverStart, flyOverEnd, t);
            transform.position = pos;
            transform.LookAt(center);
            yield return null;
        }

        flyingOver = false;
        flyOverDone = true;

        // Snap back to player
        if (target != null) SnapToTarget();
    }

    private Bounds GetCourseBounds()
    {
        Bounds b = new Bounds(target != null ? target.position : Vector3.zero, Vector3.one * 5f);
        Transform root = gridRoot != null ? gridRoot : (target != null ? target.parent : null);
        if (root != null)
        {
            foreach (Renderer r in root.GetComponentsInChildren<Renderer>())
            {
                b.Encapsulate(r.bounds);
            }
        }
        return b;
    }

    void LateUpdate()
    {
        if (target == null) return;
        if (flyingOver) return; // hands off during fly-over

        HandleInput();

        float dt = Time.deltaTime;
        if (!initialized)
        {
            smoothPivot = target.position + Vector3.up * targetHeight;
            initialized = true;
            SnapToTarget();
            return;
        }

        Quaternion orbit = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 flatForward = Quaternion.Euler(0f, yaw, 0f) * Vector3.forward;
        Vector3 pivot = target.position + Vector3.up * targetHeight + flatForward * lookAhead;

        float followT = 1f - Mathf.Exp(-followSmooth * dt);
        smoothPivot = Vector3.Lerp(smoothPivot, pivot, followT);

        Vector3 desired = smoothPivot - orbit * Vector3.forward * distance;
        transform.position = Vector3.Lerp(transform.position, desired, followT);

        Quaternion look = Quaternion.LookRotation(smoothPivot - transform.position, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, look, 1f - Mathf.Exp(-rotationSmooth * dt));
    }

    private void SnapToTarget()
    {
        Quaternion orbit = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 flatForward = Quaternion.Euler(0f, yaw, 0f) * Vector3.forward;
        Vector3 pivot = target.position + Vector3.up * targetHeight + flatForward * lookAhead;
        smoothPivot = pivot;
        transform.position = pivot - orbit * Vector3.forward * distance;
        transform.rotation = Quaternion.LookRotation(pivot - transform.position, Vector3.up);
    }

    private void HandleInput()
    {
        float dt = Time.unscaledDeltaTime;
        bool usedNewInput = false;

#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null)
        {
            usedNewInput = true;
            if (Keyboard.current.qKey.isPressed) yaw -= keyboardRotateSpeed * dt;
            if (Keyboard.current.eKey.isPressed) yaw += keyboardRotateSpeed * dt;
        }

        if (Mouse.current != null)
        {
            usedNewInput = true;

            // Check if pointer started over UI
            bool isOverUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                isDraggingMouse = !isOverUI;
            }
            if (Mouse.current.rightButton.wasPressedThisFrame || Mouse.current.middleButton.wasPressedThisFrame)
            {
                isDraggingMouse = true;
            }
            if (!Mouse.current.leftButton.isPressed && !Mouse.current.rightButton.isPressed && !Mouse.current.middleButton.isPressed)
            {
                isDraggingMouse = false;
            }

            if (isDraggingMouse)
            {
                Vector2 d = Mouse.current.delta.ReadValue();
                yaw += d.x * mouseSensitivity;
                pitch -= d.y * mouseSensitivity;
            }

            Vector2 scroll = Mouse.current.scroll.ReadValue();
            if (Mathf.Abs(scroll.y) > 0.01f)
            {
                distance = Mathf.Clamp(distance - scroll.y * zoomSensitivity, minDistance, maxDistance);
            }

            // Trackpad two-finger horizontal pan rotates the camera.
            if (Mathf.Abs(scroll.x) > 0.01f)
            {
                yaw += scroll.x * trackpadYawSensitivity;
            }
        }
#endif

        if (!usedNewInput)
        {
            try
            {
                bool isOverUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
                if (Input.GetMouseButtonDown(0)) isDraggingMouse = !isOverUI;
                if (Input.GetMouseButtonDown(1) || Input.GetMouseButtonDown(2)) isDraggingMouse = true;
                if (!Input.GetMouseButton(0) && !Input.GetMouseButton(1) && !Input.GetMouseButton(2)) isDraggingMouse = false;

                if (isDraggingMouse)
                {
                    yaw += Input.GetAxis("Mouse X") * 3.5f;
                    pitch -= Input.GetAxis("Mouse Y") * 3.5f;
                }

                float scroll = Input.GetAxis("Mouse ScrollWheel");
                if (Mathf.Abs(scroll) > 0.001f)
                    distance = Mathf.Clamp(distance - scroll * 3f, minDistance, maxDistance);

                if (Input.GetKey(KeyCode.Q)) yaw -= keyboardRotateSpeed * dt;
                if (Input.GetKey(KeyCode.E)) yaw += keyboardRotateSpeed * dt;
            }
            catch { }
        }

        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
        if (yaw > 360f) yaw -= 360f;
        if (yaw < -360f) yaw += 360f;
        if (Mathf.Abs(yaw) < 0.001f) yaw = 0f;
        distance = Mathf.Clamp(distance, minDistance, maxDistance);
    }
}
