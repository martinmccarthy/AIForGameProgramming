using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class LeaderboardDisplay : MonoBehaviour
{
    [SerializeField] private GameObject leaderboardItem;
    [SerializeField] private Transform entriesContainer;

    [SerializeField] private int numberOfScores = 10;

    public List<ScoreEntry> scores = new List<ScoreEntry>();

    private void OnEnable()
    {
        if (FirebaseManager.Instance == null)
        {
            Debug.LogWarning("FirebaseManager not found.");
            return;
        }

        FirebaseManager.Instance.RetrieveTopScores(numberOfScores, OnScoresRetrieved);
    }
    private void OnScoresRetrieved(bool success, string json)
    {
        if (!success) return;

        scores.Clear();

        string wrapped = "{\"items\":" + json + "}";
        FirestoreResultWrapper wrapper = JsonUtility.FromJson<FirestoreResultWrapper>(wrapped);

        if (wrapper?.items == null) return;

        foreach (FirestoreResult result in wrapper.items)
        {
            if (result.document == null) continue;

            scores.Add(new ScoreEntry
            {
                name = result.document.fields.name.stringValue,
                score = int.Parse(result.document.fields.score.integerValue)
            });
        }

        Debug.Log($"Loaded {scores.Count} scores.");
        PopulateLeaderboard();
    }

    void PopulateLeaderboard()
    {
        Transform parent = entriesContainer != null ? entriesContainer : transform;

        foreach (Transform child in parent)
            Destroy(child.gameObject);

        foreach (ScoreEntry entry in scores)
        {
            GameObject row = Instantiate(leaderboardItem, parent);
            row.transform.Find("Name").GetComponent<TMP_Text>().text = entry.name;
            row.transform.Find("Score").GetComponent<TMP_Text>().text = entry.score.ToString();
        }
    }

    // Firestore REST response shape
    [Serializable] private class FirestoreResultWrapper { public FirestoreResult[] items; }
    [Serializable] private class FirestoreResult { public FirestoreDocument document; }
    [Serializable] private class FirestoreDocument { public FirestoreFields fields; }
    [Serializable] private class FirestoreFields { public StringField name; public IntField score; }
    [Serializable] private class StringField { public string stringValue; }
    [Serializable] private class IntField { public string integerValue; }
}

[Serializable]
public class ScoreEntry
{
    public string name;
    public int score;
}
