using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneTransitionManager : MonoBehaviour
{
    public FadeScreen fadeScreen;
    public static SceneTransitionManager singleton;


    private void Awake()
    {
        if (singleton && singleton != this)
        {
            Destroy(gameObject);
            return;
        }

        singleton = this;
        DontDestroyOnLoad(gameObject);

        if (fadeScreen == null)
            fadeScreen = GetComponentInChildren<FadeScreen>();
    }

    public void GoToScene(int sceneIndex)
    {
        StartCoroutine(GoToSceneRoutine(sceneIndex));
    }

    IEnumerator GoToSceneRoutine(int sceneIndex)
    {
        fadeScreen.FadeOut();
        yield return new WaitForSeconds(fadeScreen.fadeDuration);

        //Launch the new scene
        SceneManager.LoadScene(sceneIndex);
    }

    public void GoToSceneAsync(int sceneIndex)
    {
        StartCoroutine(GoToSceneAsyncRoutine(sceneIndex));
    }

    IEnumerator GoToSceneAsyncRoutine(int sceneIndex)
    {   
        float duration = 1f;

        if (fadeScreen != null)
        {
            fadeScreen.FadeOut();
            duration = fadeScreen.fadeDuration;
        }
        //Launch the new scene
        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneIndex);
        operation.allowSceneActivation = false;

        float timer = 0;
        while(timer <= duration && !operation.isDone)
        {
            timer += Time.deltaTime;
            yield return null;
        }

        operation.allowSceneActivation = true;
    }
}
