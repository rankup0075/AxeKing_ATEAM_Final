using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

public class StoryCutsceneController : MonoBehaviour
{
    [Header("Slides")]
    public Image[] slides;
    public float slideDuration = 3f;

    [Header("Skip Settings")]
    public KeyCode skipKey = KeyCode.Space;
    public string nextScene = "Town";

    private int currentSlide = 0;
    private bool isSkipping = false;

    void Start()
    {
        foreach (var s in slides) s.gameObject.SetActive(false);
        StartCoroutine(PlaySlides());
    }

    IEnumerator PlaySlides()
    {
        while (currentSlide < slides.Length)
        {
            slides[currentSlide].gameObject.SetActive(true);
            yield return new WaitForSeconds(slideDuration);
            slides[currentSlide].gameObject.SetActive(false);
            currentSlide++;
        }
        LoadNextScene();
    }

    void Update()
    {
        if (Input.GetKeyDown(skipKey))
        {
            isSkipping = true;
            StopAllCoroutines();
            LoadNextScene();
        }
    }

    void LoadNextScene()
    {
        GameManager.Instance.BeginTransition(TransitionKind.None, nextScene);
        SceneManager.LoadScene(nextScene);
    }
}
