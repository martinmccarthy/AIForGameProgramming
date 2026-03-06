using UnityEngine;
using System.Collections;

public class TimeManager : MonoBehaviour
{
    public static TimeManager instance {get; private set;}

    [SerializeField]private float slowMotionScale =  0.3f; //affects how slow bullet time is- 0 is frozen, 1 is normal speed
    [SerializeField]private float slowMoTime = 10f; //time duration for bullet time in seconds
    [SerializeField]private float rechargeRate = .5f; //rate of recharge (per frame)
    private float normalTimeScale = 1f; //normal time scale
    private bool isSlowMotion = false; //flag to check if bullet time is active
    private float remainingSlowMo; //time (charge) remaining in bullet time gauge

    //on start up- ensures that timeManager persists between scenes and kills duplicates
    private void Awake()
    {
        //if timeManager doesn't exist
        if (instance == null)
        {
            //creates timeManager and persists
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        //if timeManager exists
        else
        {
            //destroys duplicate timeManagers if they spawn
            Destroy(gameObject);
        }
    }

    //sets remainingSlowMo timer equal to slowMoTime on startup
    private void Start()
    {
        remainingSlowMo = slowMoTime;
    }

    //every update (frame)
    private void Update()
    {
        if (isSlowMotion)
        {
            //drains how much slow mo time is remaining
            remainingSlowMo -= Time.unscaledDeltaTime;
            if (remainingSlowMo <= 0f) //disables slow mo if remainingslowmo reaches zero
            {
                remainingSlowMo = 0f;
                disableSlowMo();
            }
        }
        else
        {
            //recharges each frame
            remainingSlowMo += rechargeRate * Time.unscaledDeltaTime;
            if (remainingSlowMo >= slowMoTime) //stops remaining charge if it reaches slowmotime threshold
            {
                remainingSlowMo = slowMoTime;
            }
        }
    }

    //enables slow motion
    private void enableSlowMo()
    {
        Time.timeScale = slowMotionScale;
        Time.fixedDeltaTime = 0.02f * Time.timeScale;
        isSlowMotion = true;
    }

    //disables slow motion
    private void disableSlowMo()
    {
        Time.timeScale = normalTimeScale;
        Time.fixedDeltaTime = 0.02f;
        isSlowMotion = false;
    }

    //toggles slow motion
    public void toggleSlowMo()
    {
        if (isSlowMotion)
        {
            disableSlowMo();
        }
        else if(remainingSlowMo > 0f)
        {
            enableSlowMo();
        }
    }

    //returns status of slow motion
    public bool isSlowMoActive()
    {
        return isSlowMotion;
    }

    //returns how much gauge is remaining
    public float getRemainingSlowMo()
    {
        return remainingSlowMo;
    }
}