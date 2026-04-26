using UnityEngine;

public class EnemyNameplate : MonoBehaviour
{
    private Transform _cam;

    private void LateUpdate()
    {
        if (_cam == null) _cam = Camera.main?.transform;
        if (_cam == null) return;
        transform.rotation = _cam.rotation;
    }
}
