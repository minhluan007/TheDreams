using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MapTransitionManager : MonoBehaviour
{
    public static MapTransitionManager Instance;

    [SerializeField] private Image fadePanel;
    [SerializeField] private float fadeDuration = 0.3f;

    private string spawnPointName;
    private bool isLoading;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        DontDestroyOnLoad(gameObject);

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    public void LoadMap(string sceneName, string spawnPoint)
    {
        if (isLoading)
            return;

        spawnPointName = spawnPoint;

        StartCoroutine(LoadMapRoutine(sceneName));
    }

    private IEnumerator LoadMapRoutine(string sceneName)
    {
        isLoading = true;

        yield return StartCoroutine(Fade(1f));

        AsyncOperation operation =
            SceneManager.LoadSceneAsync(sceneName);

        while (!operation.isDone)
        {
            yield return null;
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!string.IsNullOrEmpty(spawnPointName))
        {
            GameObject spawnPoint =
                GameObject.Find(spawnPointName);

            GameObject player =
                GameObject.FindGameObjectWithTag("Player");

            if (spawnPoint != null && player != null)
            {
                player.transform.position =
                    spawnPoint.transform.position;
            }
        }

        spawnPointName = null;

        StartCoroutine(FadeIn());
    }

    private IEnumerator FadeIn()
    {
        yield return StartCoroutine(Fade(0f));

        isLoading = false;
    }

    private IEnumerator Fade(float targetAlpha)
    {
        if (fadePanel == null)
            yield break;

        Color color = fadePanel.color;
        float startAlpha = color.a;

        float time = 0f;

        while (time < fadeDuration)
        {
            time += Time.deltaTime;

            color.a = Mathf.Lerp(
                startAlpha,
                targetAlpha,
                time / fadeDuration
            );

            fadePanel.color = color;

            yield return null;
        }

        color.a = targetAlpha;
        fadePanel.color = color;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
}