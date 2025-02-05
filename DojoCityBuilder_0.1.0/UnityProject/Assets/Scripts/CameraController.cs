using UnityEngine;

public class CameraController : MonoBehaviour
{
    public float moveSpeed = 10f;  // Speed for moving the camera with WASD
    public float tiltSpeed = 50f;   // Speed for tilting the camera with R and F
    public float zoomSpeed = 10f;   // Speed for zooming the camera with T and G
    public float maxTilt = 85f;     // Max tilt angle to prevent flipping

    private Vector3 cameraRotation;

    void Start()
    {
        cameraRotation = transform.eulerAngles; // Store initial rotation
    }

    void Update()
    {
        HandleMovement();
        HandleZoom();
        HandleTilt();
    }

    void HandleMovement()
    {
        // Move the camera based on WASD input
        float moveX = Input.GetAxis("Horizontal"); // A and D for horizontal movement
        float moveY = Input.GetAxis("Vertical");   // W and S for forward/backward movement

        // Calculate the new position based only on x and z axes
        Vector3 rightMovement = transform.right * moveX; // Right movement based on camera's right direction
        Vector3 forwardMovement = transform.forward * moveY; // Forward movement based on camera's forward direction

        // Set the y component to 0 to ensure movement is only in the x and z directions
        rightMovement.y = 0;
        forwardMovement.y = 0;

        // Normalize the movement vector to prevent faster diagonal movement
        Vector3 move = (rightMovement + forwardMovement).normalized; 

        transform.position += move * moveSpeed * Time.deltaTime; // Move the camera
    }

    void HandleZoom()
    {
        // Zoom the camera in and out with T and G (moving along the Z-axis)
        float zoom = 0f;

        if (Input.GetKey(KeyCode.T)) // Zoom in (move camera forward)
        {
            zoom = zoomSpeed * Time.deltaTime;
        }
        else if (Input.GetKey(KeyCode.G)) // Zoom out (move camera backward)
        {
            zoom = -zoomSpeed * Time.deltaTime;
        }

        // Apply zoom movement
        transform.position += transform.forward * zoom; // Moves the camera forward/backward along its forward axis
    }

    void HandleTilt()
    {
        // Tilting the camera up and down with R and F, and left and right with Q and E
        float tiltVertical = 0f;
        float tiltHorizontal = 0f;

        if (Input.GetKey(KeyCode.R)) // Tilt up
        {
            tiltVertical = tiltSpeed * Time.deltaTime;
        }
        else if (Input.GetKey(KeyCode.F)) // Tilt down
        {
            tiltVertical = -tiltSpeed * Time.deltaTime;
        }

        if (Input.GetKey(KeyCode.Q)) // Tilt left
        {
            tiltHorizontal = tiltSpeed * Time.deltaTime;
        }
        else if (Input.GetKey(KeyCode.E)) // Tilt right
        {
            tiltHorizontal = -tiltSpeed * Time.deltaTime;
        }

        // Apply vertical tilt rotation
        cameraRotation.x += tiltVertical;

        // Apply horizontal tilt rotation
        cameraRotation.y += tiltHorizontal;

        // Clamp vertical tilt angle to prevent flipping over
        cameraRotation.x = Mathf.Clamp(cameraRotation.x, -maxTilt, maxTilt);

        // Apply rotation to the camera
        transform.eulerAngles = cameraRotation;
    }
}
