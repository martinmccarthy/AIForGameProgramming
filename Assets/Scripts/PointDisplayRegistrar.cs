using TMPro;
using UnityEngine;

[RequireComponent(typeof(TMP_Text))]
public class PointDisplayRegistrar : MonoBehaviour
{
    private void Start()
    {
        if (PointManager.Instance != null)
            PointManager.Instance.RegisterDisplay(GetComponent<TMP_Text>());
    }
}
