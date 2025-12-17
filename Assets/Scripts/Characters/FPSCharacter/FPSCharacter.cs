using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Unity.Netcode;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

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

    [Header("Touch Controls")]
    public bool enableTouchSplitControls = true;
    public float virtualStickRadiusPx = 120f;    // how far you must drag to hit full speed
    public float touchLookSensitivity = 0.12f;   // scales touch delta
    public float touchDeadzonePx = 6f;

    int _moveFingerId = -1;
    int _lookFingerId = -1;
    Vector2 _moveStartPos;

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

        if (enableTouchSplitControls)
            EnhancedTouchSupport.Enable();
    }

    void OnDisable()
    {
        if (enableTouchSplitControls)
            EnhancedTouchSupport.Disable();
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
        // 1) Default (PC / editor): actions drive everything
        Vector2 move = moveAction.ReadValue<Vector2>();
        Vector2 look = lookAction.ReadValue<Vector2>() * 0.05f; // your existing scaling

        // 2) Touch override (mobile): split left/right
        if (enableTouchSplitControls && Touchscreen.current != null)
        {
            ApplySplitTouch(ref move, ref look);
        }

        moveValue = move;
        
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

        // Handles Rotation
        float lookX = look.x;
        float lookY = look.y;

        rotationX += lookY * lookSpeed;
        rotationX = Mathf.Clamp(rotationX, -lookXLimit, lookXLimit);
        playerCamera.transform.localRotation = Quaternion.Euler(-rotationX, 0, 0);

        transform.rotation *= Quaternion.Euler(0, lookX * lookSpeed, 0);
    }

    void ApplySplitTouch(ref Vector2 moveOut, ref Vector2 lookOut)
    {
        float half = Screen.width * 0.5f;

        // Clear stale finger ids if those touches ended
        bool moveStillActive = false;
        bool lookStillActive = false;

        foreach (var t in Touch.activeTouches)
        {
            if (t.finger.index == _moveFingerId) moveStillActive = true;
            if (t.finger.index == _lookFingerId) lookStillActive = true;
        }
        if (!moveStillActive) _moveFingerId = -1;
        if (!lookStillActive) _lookFingerId = -1;

        // Claim fingers based on where they BEGIN
        foreach (var t in Touch.activeTouches)
        {
            if (t.phase != UnityEngine.InputSystem.TouchPhase.Began)
                continue;

            float x = t.screenPosition.x;

            if (x < half && _moveFingerId == -1)
            {
                _moveFingerId = t.finger.index;
                _moveStartPos = t.screenPosition;
            }
            else if (x >= half && _lookFingerId == -1)
            {
                _lookFingerId = t.finger.index;
            }
        }

        // Compute MOVE from the move finger: virtual stick around start position
        moveOut = Vector2.zero;
        if (_moveFingerId != -1)
        {
            var mt = FindActiveTouchByFinger(_moveFingerId);
            if (mt.HasValue)
            {
                Vector2 deltaFromStart = mt.Value.screenPosition - _moveStartPos;

                if (deltaFromStart.magnitude < touchDeadzonePx)
                {
                    moveOut = Vector2.zero;
                }
                else
                {
                    Vector2 clamped = Vector2.ClampMagnitude(deltaFromStart, virtualStickRadiusPx);
                    Vector2 normalized = clamped / virtualStickRadiusPx;

                    // normalized.x = strafe, normalized.y = forward
                    moveOut = normalized;
                }
            }
        }

        // Compute LOOK from the look finger: use touch delta
        // We scale to match your existing "mouse delta * 0.05" feel
        if (_lookFingerId != -1)
        {
            var lt = FindActiveTouchByFinger(_lookFingerId);
            if (lt.HasValue)
            {
                Vector2 d = lt.Value.delta;

                if (d.magnitude < touchDeadzonePx)
                    d = Vector2.zero;

                // Convert to your expected look units (same direction as mouse delta)
                lookOut = d * touchLookSensitivity;
    }
        }
    }

    Touch? FindActiveTouchByFinger(int fingerIndex)
    {
        foreach (var t in Touch.activeTouches)
            if (t.finger.index == fingerIndex)
                return t;

        return null;
    }
}
