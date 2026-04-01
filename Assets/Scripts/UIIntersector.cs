using UnityEngine;

public class UIIntersector : MonoBehaviour
{
    [SerializeField] private string buttonName = "play";
    private void OnTriggerEnter(Collider other)
    {
        if(other.CompareTag("Sword"))
        {
            switch (buttonName)
            {
                case "play":
                    // play
                    break;
                case "options":
                    // options
                    break;
                default:
                    break;
            }
        }
    }
}
