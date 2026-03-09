using UnityEngine;

public class Oscillator : MonoBehaviour
{
    [SerializeField] private float timeToMove = 5.0f;
    [SerializeField] private float oscillationRange = 1.0f;

    [SerializeField] private Transform player;

    Vector3 locationOne;
    Vector3 locationTwo;
    private float start;

    private bool flipped = true; 

    private void Start()
    {
        locationOne = transform.position;
        locationTwo = transform.position + (Vector3.up / oscillationRange);

        start = Time.time;
    }

    // Update is called once per frame
    void Update()
    {
        float t = (Time.time - start) / timeToMove;
        

        if (transform.position == locationOne || transform.position == locationTwo)
        {
            Flip();
            t = !flipped ? 0.0001f : .9999f;
        }

        if(flipped == false)
        {
            transform.position = Vector3.Lerp(locationOne, locationTwo, t);
        }
        else
        {
            transform.position = Vector3.Lerp(locationTwo, locationOne, t);
        }

        // transform.LookAt(player); // there needs to be an offset, but it doesnt work for now based on the material and orientation
    }

    private void Flip()
    {
        flipped = !flipped;
        start = Time.time;
    }
}
