using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class FadePanel : MonoBehaviour
{
    public static FadePanel instance;
    public CanvasGroup panel;

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnLevelFinishedLoading;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnLevelFinishedLoading;
    }

    void Awake()
    {
        instance = this;
    }

    public IEnumerator FadeOut(float delay)
    {
        if (delay > 0) yield return new WaitForSeconds(delay);

        while (panel.alpha > 0)
        {
            panel.alpha -= 3 *Time.deltaTime;
            yield return null;
        }
    }

    public IEnumerator FadeIn()
    {

        while (panel.alpha < 1)
        {
            panel.alpha += 3 * Time.deltaTime;
            yield return null;
        }
    }

    void OnLevelFinishedLoading(Scene scene, LoadSceneMode mode)
    {
        panel.alpha = 1;
        StartCoroutine(FadeOut(.4f));
    }
}
