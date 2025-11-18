using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;

public class FPSCharacter : NetworkBehaviour
{
    private CharacterController characterController;
    public CharColourPicker charColourPicker;

    InputAction moveAction;
    InputAction lookAction;
    InputAction jumpAction;

    public Camera playerCamera;

    public float walkSpeed = 6f;
    public float jumpPower = 7f;
    public float gravity = 10f;
    public float lookSpeed = 2f;
    public float lookXLimit = 45f;

    public Vector2 moveValue;
    Vector3 moveDirection = Vector3.zero;
    float rotationX = 0;

    void Start()
    {
        if (!IsOwner)
        {
            return;
        }

        Cursor.lockState = CursorLockMode.Locked;

        characterController = GetComponent<CharacterController>();

        moveAction = InputSystem.actions.FindAction("Player/Move_keyboard_only");
        lookAction = InputSystem.actions.FindAction("Player/Look");
        jumpAction = InputSystem.actions.FindAction("Player/Jump");
    }

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            playerCamera.enabled = true;
        }
        else
        {
            playerCamera.enabled = false;
        }
    }

    void Update()
    {
        if (!IsOwner)
        {
            return;
        }

        //Debug.Log(NetworkObjectId);
        Move();

        if (jumpAction.WasPressedThisFrame()) { OnJump(); }
    }

    void OnJump()
    {
        Color randomColor = Random.ColorHSV();

        charColourPicker.SetColour(randomColor);
    }

    void OnControllerColliderHit(ControllerColliderHit hit) // kick soccer ball
    {
        if (hit.transform.tag == "Ball")
        {
            Rigidbody body = hit.collider.attachedRigidbody;

            if (body != null)
            {
                Vector3 pushDir = new Vector3(hit.moveDirection.x, 0, hit.moveDirection.z);

                NetworkObject ballNetObj = body.GetComponent<NetworkObject>();

                if (ballNetObj != null)
                {
                    //Debug.Log("ID Attempting to kick the ball: " + NetworkObjectId); // This will only print on the client who hit the ball
                    KickBallRpc(ballNetObj, pushDir * 5f);
                }
            }
        }
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    void KickBallRpc(NetworkObjectReference ballRef, Vector3 force)
    {
        if (ballRef.TryGet(out NetworkObject ballObject))
        {
            Rigidbody ballRb = ballObject.GetComponent<Rigidbody>();
            if (ballRb != null)
            {
                //Debug.Log("ID Kicking ball: " + NetworkObjectId); // This will only print on the server
                ballRb.AddForce(force, ForceMode.Impulse);
            }
        }
    }

    public void Move()
    {
        moveValue = moveAction.ReadValue<Vector2>();
        
        Vector3 forward = transform.TransformDirection(Vector3.forward);
        Vector3 right = transform.TransformDirection(Vector3.right);

        float currentSpeedX = walkSpeed * moveValue.y;
        float currentSpeedY = walkSpeed * moveValue.x;
        float movementDirectionY = moveDirection.y;

        moveDirection = (forward * currentSpeedX) + (right * currentSpeedY);

        if (jumpAction.WasPressedThisFrame() && characterController.isGrounded)
        {
            moveDirection.y = jumpPower;
        }
        else
        {
            moveDirection.y = movementDirectionY;
        }

        if (!characterController.isGrounded)
        {
            moveDirection.y -= gravity * Time.deltaTime;
        }

        characterController.Move(moveDirection * Time.deltaTime);

        //Handles Rotation
        Vector2 mouseInput = lookAction.ReadValue<Vector2>();
        float mouseInputX = mouseInput.x * 0.05f;
        float mouseInputY = mouseInput.y * 0.05f;

        rotationX += mouseInputY * lookSpeed;
        
        rotationX = Mathf.Clamp(rotationX, -lookXLimit, lookXLimit);
        playerCamera.transform.localRotation = Quaternion.Euler(-rotationX, 0, 0);

        transform.rotation *= Quaternion.Euler(0, mouseInputX * lookSpeed, 0);
    }
}