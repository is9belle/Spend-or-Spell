using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SceneFader : MonoBehaviour
{
    static SceneFader instance;
    CanvasGroup group;
    bool busy;

    static void Ensure()
    {
        if (instance != null) return;

        var go = new GameObject("SceneFader");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<SceneFader>();

        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999;

        instance.group = go.AddComponent<CanvasGroup>();
        instance.group.alpha = 0f;
        instance.group.blocksRaycasts = false;

        var imgGO = new GameObject("Black");
        imgGO.transform.SetParent(go.transform, false);
        var img = imgGO.AddComponent<Image>();
        img.color = Color.black;
        var rt = img.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    public static void Load(string sceneName, float duration = 0.6f)
    {
        Ensure();
        if (instance.busy) return;
        instance.StartCoroutine(instance.Run(sceneName, duration));
    }

    IEnumerator Run(string sceneName, float d)
    {
        busy = true;
        group.blocksRaycasts = true;          // blocks clicks during the fade
        yield return Fade(0f, 1f, d);

        var op = SceneManager.LoadSceneAsync(sceneName);
        while (!op.isDone) yield return null;

        Time.timeScale = 1f;
        yield return Fade(1f, 0f, d);

        group.blocksRaycasts = false;
        busy = false;
    }

    IEnumerator Fade(float from, float to, float d)
    {
        float t = 0f;
        while (t < d)
        {
            t += Time.unscaledDeltaTime;
            group.alpha = Mathf.Lerp(from, to, t / d);
            yield return null;
        }
        group.alpha = to;
    }
}