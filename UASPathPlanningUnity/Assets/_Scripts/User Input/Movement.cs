using UnityEngine;

public class Movement : MonoBehaviour {
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 8f;
    [SerializeField] private float sprintMultiplier = 10f;
    [SerializeField] private float verticalSpeed = 6f;

    [Header("Look")]
    [SerializeField] private float mouseSensitivity = 2f;
    [SerializeField] private float maxLookAngle = 90f;

    [Header("UI")]
    [SerializeField] private GameObject centerUI;

    private float pitch = 0f;
    private float yaw = 0f;

    private void Start() {
        Vector3 startRotation = transform.eulerAngles;
        pitch = startRotation.x;
        yaw = startRotation.y;

        UnlockCursor();
    }

    private void Update() {
        HandleCursorToggle();

        if (Cursor.lockState == CursorLockMode.Locked) {
            HandleMouseLook();
        }

        HandleMovement();
    }

    private void HandleCursorToggle() {
        if (Input.GetMouseButtonDown(1))
        {
            if (Cursor.lockState == CursorLockMode.Locked)
                UnlockCursor();
            else
                LockCursor();
        }
    }

    private void LockCursor() {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (centerUI != null)
            centerUI.SetActive(true);
    }

    private void UnlockCursor() {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (centerUI != null)
            centerUI.SetActive(false);
    }

    private void HandleMouseLook() {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        yaw += mouseX;
        pitch -= mouseY;
        pitch = Mathf.Clamp(pitch, -maxLookAngle, maxLookAngle);

        transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
    }

    private void HandleMovement() {
        float currentSpeed = moveSpeed;
        float currentVerticalSpeed = verticalSpeed;

        if (Input.GetKey(KeyCode.LeftShift)) {
            currentSpeed *= sprintMultiplier;
            currentVerticalSpeed *= sprintMultiplier;
        }

        float horizontal = Input.GetAxisRaw("Horizontal");
        float forward = Input.GetAxisRaw("Vertical");

        Vector3 move = transform.forward * forward + transform.right * horizontal;
        move = move.normalized * currentSpeed;

        float verticalMove = 0f;

        if (Input.GetKey(KeyCode.E))
            verticalMove += currentVerticalSpeed;

        if (Input.GetKey(KeyCode.Q))
            verticalMove -= currentVerticalSpeed;

        Vector3 finalMove = move + Vector3.up * verticalMove;

        transform.position += finalMove * Time.deltaTime;
    }
}