using UnityEngine;
using UnityEngine.InputSystem;

public class TestStub : MonoBehaviour
{
    private void Update()
    {
        if (Keyboard.current.kKey.wasPressedThisFrame)
        {
            Debug.Log("[TestStub] Simulating boss death...");
            roundManager.instance.OnBossDefeated();
        }

        if (Keyboard.current.lKey.wasPressedThisFrame)
        {
            Debug.Log("[TestStub] Simulating player death...");
            roundManager.instance.OnPlayerDied();
        }
    }
}