using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class FirebaseManager : MonoBehaviour
{
    public static FirebaseManager Instance { get; private set; }

    private const string ProjectId = "ai4game-5cb9b";
    [SerializeField] private string webApiKey;
    [SerializeField] private string collectionName;
    [SerializeField] private string scoresCollectionName;

    private string InsertUrl => $"https://firestore.googleapis.com/v1/projects/{ProjectId}/databases/(default)/documents/{collectionName}?key={webApiKey}";
    private string ScoresInsertUrl => $"https://firestore.googleapis.com/v1/projects/{ProjectId}/databases/(default)/documents/{scoresCollectionName}?key={webApiKey}";
    private string ScoresQueryUrl => $"https://firestore.googleapis.com/v1/projects/{ProjectId}/databases/(default)/documents:runQuery?key={webApiKey}";

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void InsertScoreData(string name, int score, Action<bool> onComplete = null)
    {
        StartCoroutine(InsertScore(name, score, onComplete));
    }

    private IEnumerator InsertScore(string name, int score, Action<bool> onComplete)
    {
        string jsonData = $@"{{
            ""fields"": {{
                ""name"":  {{""stringValue"": ""{name}""}},
                ""score"": {{""integerValue"": ""{score}""}}
            }}
        }}";

        yield return SendRequest(ScoresInsertUrl, jsonData, (success, responseText) =>
        {
            if (success)
                Debug.Log($"Score inserted: {name} {score}");
            else
                Debug.LogError($"Failed to insert score: {responseText}");
            onComplete?.Invoke(success);
        });
    }

    public void RetrieveTopScores(int number, Action<bool, string> onComplete = null)
    {
        StartCoroutine(RetrieveScores(number, onComplete));
    }

    private IEnumerator RetrieveScores(int number, Action<bool, string> onComplete)
    {
        if (string.IsNullOrEmpty(scoresCollectionName))
        {
            Debug.LogError("[FirebaseManager] scoresCollectionName is not set in the Inspector.");
            onComplete?.Invoke(false, "scoresCollectionName is empty");
            yield break;
        }

        string jsonData = $@"{{
            ""structuredQuery"": {{
                ""from"": [{{""collectionId"": ""{scoresCollectionName}""}}],
                ""orderBy"": [{{""field"": {{""fieldPath"": ""score""}}, ""direction"": ""DESCENDING""}}],
                ""limit"": {number}
            }}
        }}";

        yield return SendRequest(ScoresQueryUrl, jsonData, (success, results) =>
        {
            if (success)
                Debug.Log($"Retrieved top {number} scores: {results}");
            else
                Debug.LogError($"Failed to retrieve scores: {results}");
            onComplete?.Invoke(success, results);
        });
    }
        

    public void InsertSurveyData(SurveyData payload, Action<bool> onComplete = null)
    {
        StartCoroutine(Insert(payload, onComplete));
    }

    private IEnumerator Insert(SurveyData payload, Action<bool> onComplete)
    {
        string questionsJson = "";
        for (int i = 0; i < payload.questions.Count; i++)
            questionsJson += $"{{\"stringValue\":\"{payload.questions[i]}\"}}" + (i < payload.questions.Count - 1 ? "," : "");

        string responsesJson = "";
        for (int i = 0; i < payload.responses.Count; i++)
            responsesJson += $"{{\"integerValue\":\"{payload.responses[i]}\"}}" + (i < payload.responses.Count - 1 ? "," : "");

        string jsonData = $@"{{
            ""fields"": {{
                ""questions"": {{""arrayValue"": {{""values"": [{questionsJson}]}}}},
                ""responses"": {{""arrayValue"": {{""values"": [{responsesJson}]}}}},
                ""timestamp"": {{""stringValue"": ""{payload.timestamp}""}}
            }}
        }}";

        yield return SendRequest(InsertUrl, jsonData, (success, responseText) =>
        {
            if (success)
                Debug.Log("data inserted");
            else
                Debug.LogError($"Failed to insert data: {responseText}");
            onComplete?.Invoke(success);
        });
    }

    private IEnumerator SendRequest(string url, string jsonBody, Action<bool, string> onComplete)
    {
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonBody);
        using UnityWebRequest request = new UnityWebRequest(url, "POST");
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        yield return request.SendWebRequest();
        bool success = request.result == UnityWebRequest.Result.Success;
        onComplete?.Invoke(success, request.downloadHandler.text);
    }

    private IEnumerator GetRequest(string url, string jsonBody, Action<bool, string> onComplete)
    {
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonBody);
        using UnityWebRequest request = new UnityWebRequest(url, "GET");
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        yield return request.SendWebRequest();
        bool success = request.result == UnityWebRequest.Result.Success;
        onComplete?.Invoke(success, request.downloadHandler.text);
    }
}