using UnityEngine;
using System.Collections.Generic;
using TMPro;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.IO;

public class SurveyManager : MonoBehaviour
{
    int currentQuestionIdx = 0;
    List<string> questions = new()
    {
        "The instructions provided were clear regarding using my katana.",
        "The instructions provided were clear regarding how to move in the environment.",
        "The game’s difficulty felt fair and balanced.",
        "The AI felt environmentally grounded.",
        "I felt rewarded for engaging with the katana’s mechanics.",
        "The environment felt easy to move around in.",
        "I prefer teleportation and sword locomotion to just sword locomotion."
    };

    public List<GameObject> selectIcons = new(); // should maybe do this programattically but i dont feel like it, yell at me later if it doesnt work -martin

    List<int> responses = new();

    [SerializeField] TMP_Text currentQuestionText;
    [SerializeField] GameObject backButton;

    private void OnEnable()
    {
        currentQuestionText.text = questions[currentQuestionIdx];
        responses.Add(0);
    }

    private void Update()
    {
        if (currentQuestionIdx == 0 && backButton.activeSelf)
        {
            backButton.SetActive(false);
        }
        else if (currentQuestionIdx > 0 && !backButton.activeSelf)
        {
            backButton.SetActive(true);
        }
    }

    public void NextQuestion()
    {
        bool answered = CheckIfAnswered();

        if (answered && currentQuestionIdx < questions.Count - 1)
        {
            currentQuestionIdx++;

            if (currentQuestionIdx >= responses.Count)
            {
                responses.Add(0);
            }
            HideSelectedUI();
            currentQuestionText.text = questions[currentQuestionIdx];
            ShowSelectedUI();
        }
        else if(answered && currentQuestionIdx == questions.Count - 1)
        {
            SurveyData data = new()
            {
                timestamp = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                questions = questions,
                responses = responses
            };

            SaveResponsesToJson(data);
            FirebaseManager.Instance.InsertSurveyData(data);
        }
    }

    private void SaveResponsesToJson(SurveyData data)
    {
        string json = JsonUtility.ToJson(data, true);
        string path = Application.persistentDataPath + "/survey_responses.json";
        
        File.WriteAllText(path, json);
    }

    public void PreviousQuestion()
    {
        if (currentQuestionIdx > 0)
        {
            HideSelectedUI();
            currentQuestionIdx--;
            currentQuestionText.text = questions[currentQuestionIdx];
            ShowSelectedUI();
        }
    }

    public void SetScoreForCurrentQuestion(int score)
    {
        HideSelectedUI();

        GameObject clickedButton = EventSystem.current.currentSelectedGameObject;
        clickedButton.transform.Find("Selected").gameObject.SetActive(true);

        responses[currentQuestionIdx] = score;
    }

    void HideSelectedUI()
    {
        foreach(GameObject b in selectIcons)
        {
            b.SetActive(false);
        }
    }
    
    void ShowSelectedUI()
    {
        if (responses[currentQuestionIdx] > 0)
        {
            selectIcons[responses[currentQuestionIdx] - 1].SetActive(true);
        }
    }

    bool CheckIfAnswered()
    {
        return responses[currentQuestionIdx] > 0;
    }
}
