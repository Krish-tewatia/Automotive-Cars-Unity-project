using UnityEngine;

/// <summary>
/// Smooth orbit camera controller for showcasing the car.
/// Supports mouse drag rotation, scroll zoom, and auto-rotation.
/// </summary>
public class CameraOrbitController : MonoBehaviour
{
    [Header("Target")]
    public Transform target; // The car to orbit around
    public Vector3 targetOffset = new Vector3(0f, 0.5f, 0f);

    [Header("Orbit Settings")]
    public float distance = 6f;
    public float minDistance = 3f;
    public float maxDistance = 12f;
    public float orbitSpeed = 5f;
    public float zoomSpeed = 2f;
    public float smoothSpeed = 8f;

    [Header("Angle Limits")]
    public float minVerticalAngle = 5f;
    public float maxVerticalAngle = 60f;

    [Header("Auto Rotation")]
    public bool autoRotate = true;
    public float autoRotateSpeed = 10f;
    public float autoRotateDelay = 3f; // Seconds of inactivity before auto-rotate

    [Header("Damping")]
    public float rotationDamping = 5f;

    private float currentHorizontalAngle = 0f;
    private float currentVerticalAngle = 25f;
    private float currentDistance;
    private float targetHorizontalAngle;
    private float targetVerticalAngle;
    private float targetDistance;
    private float lastInputTime;
    private bool isDragging = false;

    private Vector3 currentVelocity;

    private void Start()
    {
        currentDistance = distance;
        targetDistance = distance;
        targetHorizontalAngle = currentHorizontalAngle;
        targetVerticalAngle = currentVerticalAngle;
        lastInputTime = -autoRotateDelay; // Allow auto-rotate immediately

        if (target == null)
        {
            Debug.LogWarning("[CameraOrbit] No target assigned. Looking for object tagged 'Car'...");
            GameObject car = GameObject.FindGameObjectWithTag("Car");
            if (car != null) target = car.transform;
        }
    }

    private void LateUpdate()
    {
        if (target == null) return;

        HandleInput();
        HandleAutoRotation();
        UpdateCameraPosition();
    }

    private void HandleInput()
    {
        // Mouse drag rotation
        if (Input.GetMouseButtonDown(0))
        {
            isDragging = true;
        }
        if (Input.GetMouseButtonUp(0))
        {
            isDragging = false;
        }

        if (isDragging)
        {
            float mouseX = Input.GetAxis("Mouse X");
            float mouseY = Input.GetAxis("Mouse Y");

            if (Mathf.Abs(mouseX) > 0.01f || Mathf.Abs(mouseY) > 0.01f)
            {
                targetHorizontalAngle += mouseX * orbitSpeed;
                targetVerticalAngle -= mouseY * orbitSpeed;
                targetVerticalAngle = Mathf.Clamp(targetVerticalAngle, minVerticalAngle, maxVerticalAngle);
                lastInputTime = Time.time;
            }
        }

        // Scroll zoom
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.01f)
        {
            targetDistance -= scroll * zoomSpeed;
            targetDistance = Mathf.Clamp(targetDistance, minDistance, maxDistance);
            lastInputTime = Time.time;
        }
    }

    private void HandleAutoRotation()
    {
        if (!autoRotate) return;

        if (Time.time - lastInputTime > autoRotateDelay && !isDragging)
        {
            targetHorizontalAngle += autoRotateSpeed * Time.deltaTime;
        }
    }

    private void UpdateCameraPosition()
    {
        // Smooth interpolation
        currentHorizontalAngle = Mathf.LerpAngle(currentHorizontalAngle, targetHorizontalAngle, Time.deltaTime * smoothSpeed);
        currentVerticalAngle = Mathf.Lerp(currentVerticalAngle, targetVerticalAngle, Time.deltaTime * smoothSpeed);
        currentDistance = Mathf.Lerp(currentDistance, targetDistance, Time.deltaTime * smoothSpeed);

        // Calculate position
        Quaternion rotation = Quaternion.Euler(currentVerticalAngle, currentHorizontalAngle, 0);
        Vector3 position = target.position + targetOffset + rotation * new Vector3(0, 0, -currentDistance);

        // Apply with smoothing
        transform.position = Vector3.SmoothDamp(transform.position, position, ref currentVelocity, 1f / smoothSpeed);
        transform.LookAt(target.position + targetOffset);
    }

    /// <summary>
    /// Smoothly transition to a specific view angle
    /// </summary>
    public void SetViewAngle(float horizontal, float vertical, float dist = -1f)
    {
        targetHorizontalAngle = horizontal;
        targetVerticalAngle = Mathf.Clamp(vertical, minVerticalAngle, maxVerticalAngle);
        if (dist > 0) targetDistance = Mathf.Clamp(dist, minDistance, maxDistance);
        lastInputTime = Time.time;
    }

    /// <summary>
    /// Quick view presets
    /// </summary>
    public void SetFrontView() => SetViewAngle(0, 20, 6);
    public void SetSideView() => SetViewAngle(90, 15, 5);
    public void SetRearView() => SetViewAngle(180, 20, 6);
    public void SetTopView() => SetViewAngle(0, 55, 8);
}
