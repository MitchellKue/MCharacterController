using UnityEngine;

public class ThirdPersonFixedTiltCamera : MonoBehaviour
{
    [Header("Target")]
    public Transform target;

    [Tooltip("How quickly the camera follows the target position")]
    public float followSpeed = 10f;

    [Header("Distance")]
    [Tooltip("Current distance from target")]
    public float distance = 6f;
    [Tooltip("Minimum zoom distance")]
    public float minDistance = 3f;
    [Tooltip("Maximum zoom distance")]
    public float maxDistance = 10f;
    [Tooltip("Speed of zoom when using mouse wheel or Q/E")]
    public float zoomSpeed = 5f;

    [Header("Rotation")]
    [Tooltip("Horizontal orbit speed (degrees per second) when using A/D")]
    public float orbitSpeed = 90f;
    [Tooltip("Fixed vertical tilt angle (degrees, e.g., 20-40)")]
    public float fixedTiltAngle = 30f;

    [Header("Height")]
    [Tooltip("Fixed Y height of the camera in world space")]
    public float cameraHeight = 2.0f;

    [Header("Smoothing")]
    [Tooltip("Lerp factor for rotation smoothing")]
    public float rotationSmoothSpeed = 10f;

    private float currentYaw;      // around Y axis
    private float targetYaw;
    private float targetDistance;

    private void Start()
    {
        if (target == null)
        {
            Debug.LogWarning("ThirdPersonFixedTiltCamera: No target assigned.");
            enabled = false;
            return;
        }

        targetDistance = Mathf.Clamp(distance, minDistance, maxDistance);
        currentYaw = targetYaw = transform.eulerAngles.y;
    }

    private void Update()
    {
        if (target == null) return;

        HandleInput();
    }

    private void LateUpdate()
    {
        if (target == null) return;

        // Smooth yaw and distance
        currentYaw = Mathf.LerpAngle(currentYaw, targetYaw, rotationSmoothSpeed * Time.deltaTime);
        distance = Mathf.Lerp(distance, targetDistance, rotationSmoothSpeed * Time.deltaTime);
        distance = Mathf.Clamp(distance, minDistance, maxDistance);

        // Rotation uses fixed tilt
        Quaternion rotation = Quaternion.Euler(fixedTiltAngle, currentYaw, 0f);

        // Position: orbit around target, but lock Y to cameraHeight
        Vector3 targetPos = target.position;
        Vector3 offsetDir = rotation * Vector3.back; // camera behind the target
        Vector3 desiredPosition = targetPos + offsetDir * distance;
        desiredPosition.y = cameraHeight; // lock vertical height

        // Smooth follow
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, followSpeed * Time.deltaTime);

        transform.position = smoothedPosition;
        transform.rotation = rotation;
    }

    private void HandleInput()
    {
        // --- Orbit with A/D around Y axis ---
        float horizontal = 0f;
        if (Input.GetKey(KeyCode.A)) horizontal -= 1f;
        if (Input.GetKey(KeyCode.D)) horizontal += 1f;
        targetYaw += horizontal * orbitSpeed * Time.deltaTime;

        // --- Zoom with mouse wheel and Q/E ---
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        float zoomInput = scroll;

        if (Input.GetKey(KeyCode.Q)) zoomInput += 1f;
        if (Input.GetKey(KeyCode.E)) zoomInput -= 1f;

        targetDistance -= zoomInput * zoomSpeed;
        targetDistance = Mathf.Clamp(targetDistance, minDistance, maxDistance);
    }
}