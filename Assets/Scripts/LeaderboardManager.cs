using TMPro;
using UnityEngine;

public class LeaderboardManager : MonoBehaviour
{
    [SerializeField] private TMP_Text letter1;
    [SerializeField] private TMP_Text letter2;
    [SerializeField] private TMP_Text letter3;

    [SerializeField] private GameObject nameEntryMenu;
    [SerializeField] private GameObject leaderboardPanel;

    public void ReturnToMainMenu()
    {
        GameManager.instance.LoadMainMenu();
    }

    public void SubmitAndShowLeaderboard()
    {
        int points = PointManager.Instance != null ? PointManager.Instance.points : 1200;
        string name = Assemble();
        FirebaseManager.Instance.InsertScoreData(name, points);

        if (nameEntryMenu != null) nameEntryMenu.SetActive(false);
        if (leaderboardPanel != null) leaderboardPanel.SetActive(true);
    }

    public void UploadScore()
    {
        int points = PointManager.Instance.points;
        string name = Assemble();
        FirebaseManager.Instance.InsertScoreData(name, points);
    }

    string Assemble()
    {
        return letter1.text + letter2.text + letter3.text;
    }
}
