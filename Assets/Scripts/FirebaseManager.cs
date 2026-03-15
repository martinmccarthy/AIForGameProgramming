using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class FirebaseManager : MonoBehaviour
{
    public static FirebaseManager Instance { get; private set; }

    private const string ProjectId = "ai4game-5cb9b";
    [SerializeField] private string collectionName;

    private string InsertUrl => $"https://firestore.googleapis.com/v1/projects/{ProjectId}/databases/(default)/documents/{collectionName}";

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
}