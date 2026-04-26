using UnityEngine;

public class EnemyNameplate : MonoBehaviour
{
    private Transform _cam;

    private void Start()
    {
        _cam = Camera.main?.transform;
    }

    private void LateUpdate()
    {
        if (_cam == null) return;
        transform.rotation = _cam.rotation;
    }
}
