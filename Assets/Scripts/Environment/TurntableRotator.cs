using UnityEngine;

/// <summary>
/// Slowly rotates the turntable/platform the car sits on.
/// Attach to the platform/turntable object.
/// </summary>
public class TurntableRotator : MonoBehaviour
{
    [Header("Rotation")]
    public float rotationSpeed = 5f;
    public bool autoRotate = true;
    public Vector3 rotationAxis = Vector3.up;

    [Header("Bobbing Effect")]
    public bool enableBobbing = false;
    public float bobHeight = 0.02f;
    public float bobSpeed = 1f;

    private Vector3 startPosition;

    private void Start()
    {
        startPosition = transform.position;
    }

    private void Update()
    {
        if (autoRotate)
        {
            transform.Rotate(rotationAxis, rotationSpeed * Time.deltaTime, Space.World);
        }

        if (enableBobbing)
        {
            float newY = startPosition.y + Mathf.Sin(Time.time * bobSpeed) * bobHeight;
            transform.position = new Vector3(transform.position.x, newY, transform.position.z);
        }
    }

    public void SetRotationSpeed(float speed)
    {
        rotationSpeed = speed;
    }

    public void ToggleRotation()
    {
        autoRotate = !autoRotate;
    }
}
