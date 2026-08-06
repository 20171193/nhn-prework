using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// 섬광탄에 맞았을 때 화면을 밝게 만드는 풀스크린 페이드. Canvas_Effect/IMG_LightFade
// (알파 0으로 시작하는 풀스크린 Image)에 부착한다.
// 알파 0 -> maxAlpha(페이드인) -> holdDuration초 유지 -> 0(페이드아웃) 순서로 재생.
public class ScreenFadeEffect : MonoBehaviour
{
    [SerializeField] private Image image;
    [SerializeField] private float maxAlpha = 220f / 255f;
    [SerializeField] private float fadeInTime = 0.15f;
    [SerializeField] private float fadeOutTime = 0.5f;

    Coroutine routine;

    public void Play(float holdDuration)
    {
        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(FadeRoutine(holdDuration));
    }

    IEnumerator FadeRoutine(float holdDuration)
    {
        yield return Fade(0f, maxAlpha, fadeInTime);
        yield return new WaitForSeconds(holdDuration);
        yield return Fade(maxAlpha, 0f, fadeOutTime);
        routine = null;
    }

    IEnumerator Fade(float from, float to, float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            SetAlpha(Mathf.Lerp(from, to, t / duration));
            yield return null;
        }
        SetAlpha(to);
    }

    void SetAlpha(float alpha)
    {
        var color = image.color;
        color.a = alpha;
        image.color = color;
    }
}
