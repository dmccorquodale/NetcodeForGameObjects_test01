using UnityEngine;
using UnityEngine.InputSystem;
using System;

public class FPSCharController : MonoBehaviour
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
        Cursor.lockState = CursorLockMode.Locked;

        characterController = GetComponent<CharacterController>();

        moveAction = InputSystem.actions.FindAction("Player/Move");
        lookAction = InputSystem.actions.FindAction("Player/Look");
        jumpAction = InputSystem.actions.FindAction("Player/Jump");
    }

    void Update ()
    {
        MoveCharacter();
    }

    void MoveCharacter()
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