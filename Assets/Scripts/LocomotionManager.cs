using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Locomotion;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;

public class LocomotionManager : MonoBehaviour
{
    [SerializeField] private float controllerDistanceThreshold = 0.1f;
    [SerializeField] private float centerThreshold = 0.3f;
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float rotationSpeed = 60f;
    [SerializeField] private float gravity = -9.81f;
    [SerializeField] private float groundedForce = -2f;

    [SerializeField] private CharacterController characterController;
    [SerializeField] private Transform xrOrigin;
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private InputManager inputManager;
    [SerializeField] private TeleportationProvider teleportationProvider;

    private float verticalVelocity;
    private bool teleportInProgress;

    private void OnEnable()
    {
        if (teleportationProvider != null)
        {
            teleportationProvider.locomotionStarted += OnTeleportStarted;
            teleportationProvider.locomotionEnded += OnTeleportEnded;
        }
    }

    private void OnDisable()
    {
        if (teleportationProvider != null)
        {
            teleportationProvider.locomotionStarted -= OnTeleportStarted;
            teleportationProvider.locomotionEnded -= OnTeleportEnded;
        }
    }

    private void Update()
    {
        if (teleportInProgress)
            return;

        ApplyGravity();

        Vector3 horizontalMove = Vector3.zero;

        if (inputManager.GetControllerDistance() < controllerDistanceThreshold)
        {
            Vector3 toHands = GetFlattenedHandDirection();
            if (toHands != Vector3.zero)
            {
                Vector3 forward = GetFlatForward();
                if (forward != Vector3.zero && Vector3.Dot(toHands, forward) > 0f)
                {
                    float lateralDot = Vector3.Dot(toHands, GetFlatRight());

                    if (Mathf.Abs(lateralDot) > centerThreshold)
                        xrOrigin.Rotate(Vector3.up, Mathf.Sign(lateralDot) * rotationSpeed * Time.deltaTime);
                    else
                        horizontalMove = forward * moveSpeed;
                }
            }
        }

        Vector3 finalMove = horizontalMove;
        finalMove.y = verticalVelocity;

        characterController.Move(finalMove * Time.deltaTime);
    }

    private void OnTeleportStarted(LocomotionProvider provider)
    {
        teleportInProgress = true;

        if (characterController != null)
            characterController.enabled = false;
    }

    private void OnTeleportEnded(LocomotionProvider provider)
    {
        if (characterController != null)
            characterController.enabled = true;

        verticalVelocity = groundedForce;
        teleportInProgress = false;
    }

    private void ApplyGravity()
    {
        if (characterController.isGrounded && verticalVelocity < 0f)
            verticalVelocity = groundedForce;
        else
            verticalVelocity += gravity * Time.deltaTime;
    }

    private Vector3 GetHandMidpoint()
    {
        Vector3 right = inputManager.GetControllerWorldPosition(false);
        Vector3 left = inputManager.GetControllerWorldPosition(true);
        return (right + left) * 0.5f;
    }

    private Vector3 GetFlattenedHandDirection()
    {
        Vector3 midpoint = GetHandMidpoint();
        Vector3 body = xrOrigin.position;

        Vector3 dir = midpoint - body;
        dir.y = 0f;

        if (dir.sqrMagnitude < 0.0001f)
            return Vector3.zero;

        return dir.normalized;
    }

    private Vector3 GetFlatForward()
    {
        Vector3 forward = cameraTransform.forward;
        forward.y = 0f;

        if (forward.sqrMagnitude < 0.0001f)
            return Vector3.zero;

        return forward.normalized;
    }

    private Vector3 GetFlatRight()
    {
        Vector3 right = cameraTransform.right;
        right.y = 0f;

        if (right.sqrMagnitude < 0.0001f)
            return Vector3.zero;

        return right.normalized;
    }

}