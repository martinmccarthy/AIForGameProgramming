using UnityEngine;

public class SwordManager : MonoBehaviour
{
    [SerializeField] private Transform head;

    [SerializeField] private float downSpeed = 1.0f;
    [SerializeField] private float fowardSpeed = 1.0f;
    [SerializeField] private float genericSpeed = 1.0f;

    private Vector3 lastPosition;
    private float lastTime;

    private void Start()
    {
        lastPosition = transform.position;
        lastTime = Time.time;
    }

    private void Update()
    {
        if (!head) return;

        float dt = Time.time - lastTime;
        if(dt <= 0) return;

        Vector3 velocity = (transform.position - lastPosition) / dt;

        Vector3 localVelocity = head.InverseTransformDirection(velocity);
        if(localVelocity.y < -downSpeed)
        {
            Debug.Log("down slash");
        }

        if(localVelocity.z > fowardSpeed)
        {
            Debug.Log("stab");
        }

        if(velocity.magnitude > genericSpeed)
        {
            Debug.Log("generic");
        }

        lastPosition = transform.position;
        lastTime = Time.time;
    }
}
