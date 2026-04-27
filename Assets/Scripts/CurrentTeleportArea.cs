using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;

public class CurrentTeleportArea : MonoBehaviour
{
    private TeleportationAnchor tpAnchor;

    private void Start()
    {
        tpAnchor = GetComponent<TeleportationAnchor>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if(other.CompareTag("Player"))
        {
            tpAnchor.enabled = false;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if(other.CompareTag("Player"))
        {
            tpAnchor.enabled = true;
        }
    }
}
