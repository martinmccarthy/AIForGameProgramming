using UnityEngine;

public class SettingsUI : MonoBehaviour
{
    [SerializeField] private GameObject menuObject;

    public void OpenMenu()
    {
        menuObject.SetActive(true);
    }

    public void CloseMenu()
    {
        menuObject.SetActive(false);
    }

    public void ToggleMenu()
    {
        menuObject.SetActive(!menuObject.activeSelf);
    }
}
