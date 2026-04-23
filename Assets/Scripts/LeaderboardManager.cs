using TMPro;
using UnityEngine;

public class LeaderboardManager : MonoBehaviour
{
    [SerializeField] private TMP_Text letter1;
    [SerializeField] private TMP_Text letter2;
    [SerializeField] private TMP_Text letter3;

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
