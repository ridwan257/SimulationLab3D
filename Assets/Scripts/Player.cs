using UnityEngine;

public class Player : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 12f;
    public float mouseSensitivity = 2f;

    [Header("References")]
    private Transform cameraTransform;
    private Rigidbody rb; // Reference to the Rigidbody
    private float xRotation = 0f;
    private Vector3 moveInput; // Store input here

    void Start()
    {
        // Get the Rigidbody component
        rb = GetComponent<Rigidbody>();

        // IMPORTANT: Set collision detection to Continuous in code or Inspector
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

        // Freeze rotation so physics doesn't tip the capsule over
        rb.freezeRotation = true;

        cameraTransform = GetComponentInChildren<Camera>().transform;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        HandleRotation();

        // Capture input in Update (more responsive)
        float x = Input.GetAxisRaw("Horizontal");
        float z = Input.GetAxisRaw("Vertical");
        moveInput = (transform.right * x) + (transform.forward * z).normalized;
    }

    // Use FixedUpdate for all Rigidbody/Physics movements
    void FixedUpdate()
    {
        HandleMovement();
    }

    private void HandleRotation()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        cameraTransform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        transform.Rotate(Vector3.up * mouseX);
    }

    private void HandleMovement()
    {
        // Calculate the target position
        Vector3 targetVelocity = moveSpeed * Time.fixedDeltaTime * moveInput;

        // MovePosition is "Wall-Friendly" movement
        rb.MovePosition(rb.position + targetVelocity);
    }
}