using UnityEngine;

public class SwordManager : MonoBehaviour
{
    // Controller Input Values
    [SerializeField] private Transform leftControllerTransform;
    [SerializeField] private Transform rightControllerTransform;
    // settings for the thresholds of speeds and what not
    [SerializeField, Range(0f, 10f)] private float MIN_SWIPE_SPEED = 1.5f;
    [SerializeField, Range(0f, 1f)] private float MIN_ANGLE_THRESHOLD = 0.6f;

    // this will allow us to determine which hand the sword should be bound to in the future based on a settings option
    private bool playerLefty = false; // playerLefty = userSettings.hand in start or something like that;
    private Transform activeController;

    private Vector3 lastPosition;
    private float lastTime;

    private void Start()
    {
        activeController = playerLefty ? leftControllerTransform : rightControllerTransform;
        lastPosition = activeController.position;
        lastTime = Time.time;
    }

    private void Update()
    {
        MotionCheck(activeController.position);
    }

    private void MotionCheck(Vector3 controllerPosition)
    {
        float currentTime = Time.time;
        float deltaTime = currentTime - lastTime;

        Vector3 velocity = (controllerPosition - lastPosition) / deltaTime;
        Vector3 direction = velocity.normalized;

        if(velocity.magnitude > MIN_SWIPE_SPEED)
        {
            return;
        }

        bool movingForward = Vector3.Dot(direction, Vector3.forward) > MIN_ANGLE_THRESHOLD; // this will probably also need to validate the rotation of the controller
        if (movingForward)
        {
            Debug.Log("Swipe Forward Detected");
        }

        bool movingDown = Vector3.Dot(direction, Vector3.down) > MIN_ANGLE_THRESHOLD;
        
        if (movingDown)
        {
            Debug.Log("Swipe Down Detected");
        }

        if(!movingDown && !movingForward)
        {
            Debug.Log("Generic Swipe Detected");
        }

        lastPosition = controllerPosition;
        lastTime = currentTime;
    }
}
