using UnityEngine;

public class Player : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed;
    public float mouseSensitivity;

    [Header("References")]
    private Transform cameraTransform;
    private Rigidbody rb;
    private float xRotation = 0f;
    private Vector3 moveInput;

    void Start()
    {
        moveSpeed = 10f;
        mouseSensitivity = 2f;

        // Others
        rb = GetComponent<Rigidbody>();
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        rb.freezeRotation = true;

        cameraTransform = GetComponentInChildren<Camera>().transform;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        HandleRotation();

        float x = Input.GetAxisRaw("Horizontal");
        float z = Input.GetAxisRaw("Vertical");
        moveInput = (transform.right * x) + (transform.forward * z).normalized;
    }

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
        Vector3 targetVelocity = moveSpeed * Time.fixedDeltaTime * moveInput;
        rb.MovePosition(rb.position + targetVelocity);
    }
}