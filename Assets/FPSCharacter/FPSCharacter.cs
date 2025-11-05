using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;

public class FPSCharacter : NetworkBehaviour
{
    private CharacterController characterController;

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

        //Cursor.lockState = CursorLockMode.Locked;

        characterController = GetComponent<CharacterController>();

        moveAction = InputSystem.actions.FindAction("Player/Move");
        lookAction = InputSystem.actions.FindAction("Player/Look");
        jumpAction = InputSystem.actions.FindAction("Player/Jump");
    }

    public override void OnNetworkSpawn()
    {
        // Listen for color changes
        PlayerColor.OnValueChanged += OnColorChanged;

        PlayerColor.Value = PickMyColor();
        // Apply current color when object spawns locally
        rendererColourToChange.material.color = PlayerColor.Value;
    }

    public override void OnDestroy()
    {
        PlayerColor.OnValueChanged -= OnColorChanged;
    }

    private void OnColorChanged(Color oldValue, Color newValue)
    {
        rendererColourToChange.material.color = newValue;
    }
    
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void SetColorServerRpc(Color newColor)
    {
        // Only the server updates the NetworkVariable
        PlayerColor.Value = newColor;
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
    
    Color PickMyColor()
    {
        Color myColour = Color.green; // Don't think we should ever see a green duck

        if (IsOwner && !IsHost) // I am the client, not the host
        {
            myColour = new Color(0f, 0.66f, 1f, 1f);
        }
        else // I am the host
        {
            myColour = new Color(1f, 0f, 0f, 1f);
        }

        //MyColorRpc(NetworkObjectId, myColour);
        return myColour;
    }
    
    /*
    [Rpc(SendTo.ClientsAndHost)]
    void MyColorRpc(ulong myNetworkObjectId, Color mycolour)
    {
        rendererColourToChange.material.color = mycolour;
    }
    */
    
    void OnJump()
    {
        Color randomColor = Random.ColorHSV();

        //MyColorRpc(NetworkObjectId, randomColor);
        PlayerColor.Value = randomColor;
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