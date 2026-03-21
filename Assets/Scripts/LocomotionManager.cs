using UnityEngine;

public class LocomotionManager : MonoBehaviour
{
    [SerializeField] private float controllerDistanceThreshold = 0.1f;
    [SerializeField] private float centerThreshold = 0.3f;
    [SerializeField] private float moveSpeed = 2f;

    [SerializeField] private CharacterController characterController;
    [SerializeField] private Transform xrOrigin;
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private InputManager inputManager;

    private void Update()
    {
        if (inputManager.GetControllerDistance() >= controllerDistanceThreshold) return;

        Vector3 toHands = GetFlattenedHandDirection();
        if (toHands == Vector3.zero) return;

        Vector3 moveDirection = GetMoveDirection(toHands);
        if (moveDirection == Vector3.zero) return;

        Move(moveDirection);
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
        return right.normalized;
    }

    private Vector3 GetMoveDirection(Vector3 toHands)
    {
        Vector3 forward = GetFlatForward();
        if (forward == Vector3.zero) return Vector3.zero;

        Vector3 right = GetFlatRight();

        float forwardDot = Vector3.Dot(toHands, forward);
        float lateralDot = Vector3.Dot(toHands, right);

        if (forwardDot <= 0f) return Vector3.zero;

        if (lateralDot > centerThreshold)
            return right;

        if (lateralDot < -centerThreshold)
            return -right;

        return forward;
    }

    private void Move(Vector3 direction)
    {
        characterController.Move(direction * moveSpeed * Time.deltaTime);
    }
}