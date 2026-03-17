using UnityEngine;

public class TopDownOrbitCamera : MonoBehaviour
{
    [Header("Target")]
    public Transform target;

    [Tooltip("How quickly the camera follows the target position")]
    public float followSpeed = 10f;

    [Header("Distance")]
    [Tooltip("Current distance from target")]
    public float distance = 10f;
    [Tooltip("Minimum distance when zooming in")]
    public float minDistance = 5f;
    [Tooltip("Maximum distance when zooming out")]
    public float maxDistance = 20f;
    [Tooltip("Speed of zoom when using mouse wheel or Q/E")]
    public float zoomSpeed = 5f;

    [Header("Rotation")]
    [Tooltip("Horizontal rotation speed (degrees per second) when using A/D")]
    public float orbitSpeed = 90f;

    [Header("Tilt")]
    [Tooltip("Vertical tilt angle in degrees (0 = flat, 90 = straight down)")]
    public float tiltAngle = 60f;
    [Tooltip("Minimum tilt angle (degrees)")]
    public float minTiltAngle = 30f;
    [Tooltip("Maximum tilt angle (degrees)")]
    public float maxTiltAngle = 80f;
    [Tooltip("Tilt speed (degrees per second) when using W/S")]
    public float tiltSpeed = 60f;

    [Header("Axis Inversion")]
    [Tooltip("Invert horizontal orbit input (A/D)")]
    public bool invertHorizontalOrbit = false;

    [Tooltip("Invert vertical tilt input (W/S)")]
    public bool invertVerticalTilt = false;

    [Tooltip("Invert zoom input (mouse wheel, Q/E)")]
    public bool invertZoom = false;

    [Header("Smoothing")]
    [Tooltip("Lerp factor for rotation & zoom smoothing")]
    public float rotationSmoothSpeed = 10f;

    private float currentYaw;     // around Y axis
    private float currentTilt;    // around X axis
    private float targetYaw;
    private float targetTilt;
    private float targetDistance;

    private void Start()
    {
        if (target == null)
        {
            Debug.LogWarning("TopDownOrbitCamera: No target assigned.");
            enabled = false;
            return;
        }

        // Initialize values
        targetDistance = Mathf.Clamp(distance, minDistance, maxDistance);
        currentYaw = targetYaw = transform.eulerAngles.y;
        currentTilt = targetTilt = tiltAngle;
    }

    private void Update()
    {
        if (target == null) return;

        HandleInput();
    }

    private void LateUpdate()
    {
        if (target == null) return;

        // Smoothly interpolate tilt, yaw, and distance
        currentYaw = Mathf.LerpAngle(currentYaw, targetYaw, rotationSmoothSpeed * Time.deltaTime);
        currentTilt = Mathf.Lerp(currentTilt, targetTilt, rotationSmoothSpeed * Time.deltaTime);
        distance = Mathf.Lerp(distance, targetDistance, rotationSmoothSpeed * Time.deltaTime);

        // Clamp tilt and distance
        currentTilt = Mathf.Clamp(currentTilt, minTiltAngle, maxTiltAngle);
        distance = Mathf.Clamp(distance, minDistance, maxDistance);

        // Compute desired rotation (X tilt, then Y yaw)
        Quaternion rotation = Quaternion.Euler(currentTilt, currentYaw, 0f);

        // Desired camera position
        Vector3 desiredPosition = target.position - (rotation * Vector3.forward * distance);

        // Smooth position follow
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, followSpeed * Time.deltaTime);

        transform.position = smoothedPosition;
        transform.rotation = rotation;
    }

    private void HandleInput()
    {
        // --- Orbit with A/D (Y axis) ---
        float horizontal = 0f;
        if (Input.GetKey(KeyCode.A)) horizontal -= 1f;
        if (Input.GetKey(KeyCode.D)) horizontal += 1f;

        if (invertHorizontalOrbit)
            horizontal = -horizontal;

        targetYaw += horizontal * orbitSpeed * Time.deltaTime;

        // --- Tilt with W/S (X axis) ---
        float vertical = 0f;
        if (Input.GetKey(KeyCode.W)) vertical += 1f;
        if (Input.GetKey(KeyCode.S)) vertical -= 1f;

        if (invertVerticalTilt)
            vertical = -vertical;

        // subtract vertical so positive = look more down (more top-down)
        targetTilt -= vertical * tiltSpeed * Time.deltaTime;

        // Clamp intended tilt
        targetTilt = Mathf.Clamp(targetTilt, minTiltAngle, maxTiltAngle);

        // --- Zoom with mouse wheel and Q/E ---
        float scroll = Input.GetAxis("Mouse ScrollWheel"); // positive = forward
        float zoomInput = scroll;

        if (Input.GetKey(KeyCode.Q)) zoomInput += 1f;
        if (Input.GetKey(KeyCode.E)) zoomInput -= 1f;

        if (invertZoom)
            zoomInput = -zoomInput;

        targetDistance -= zoomInput * zoomSpeed;
        targetDistance = Mathf.Clamp(targetDistance, minDistance, maxDistance);
    }
}