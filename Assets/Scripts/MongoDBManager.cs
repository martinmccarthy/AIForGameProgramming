using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class MongoDBManager : MonoBehaviour
{
    public static MongoDBManager Instance { get; private set; }


    [SerializeField] private string appId;
    [SerializeField] private string apiKey;
    [SerializeField] private string clusterName;
    [SerializeField] private string databaseName;
    [SerializeField] private string collectionName;

    private string FindUrl => $"https://data.mongodb-api.com/app/{appId}/endpoint/data/v1/action/find";

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
        string InsertOneUrl = $"https://data.mongodb-api.com/app/{appId}/endpoint/data/v1/action/insertOne";


        var jsonData = JsonUtility.ToJson(new
            {
                collection = collectionName,
                database = databaseName,
                dataSource = clusterName,
                document = payload
            });

        yield return SendRequest(InsertOneUrl, JsonUtility.ToJson(jsonData), (success, responseText) =>
        {
            if(success)
            {
                Debug.Log("data inserted");
            }
            else
            {
                Debug.LogError($"Failed to insert data: {responseText}");
            }
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
        request.SetRequestHeader("api-key", apiKey);

        yield return request.SendWebRequest();

        bool success = request.result == UnityWebRequest.Result.Success;
        onComplete?.Invoke(success, request.downloadHandler.text);
    }

}
