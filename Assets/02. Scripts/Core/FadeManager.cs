using System;
using System.Collections;
using Photon.Pun;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// 모든 씬 전환(로비 복귀, 싱글플레이 진입, 매칭 후 전투 씬 등)에 공용으로 쓰는
// 풀스크린 페이드 연출. 싱글턴 + DontDestroyOnLoad(Singleton<T> 기반)라 씬이 바뀌어도
// 오버레이가 계속 화면을 덮은 채로 다음 씬까지 이어진다.
//
// 씬 로드 방식이 두 갈래라 진입점도 둘로 나뉜다.
// 1. FadeAndLoad: 우리가 직접 SceneManager.LoadSceneAsync로 로드하는 일반 전환용
//    (로비 복귀, 싱글플레이 진입 등).
// 2. FadeOutThenPhotonLoad: 매칭 성사 후 전투 씬 진입용. 실제 로드(PhotonNetwork.LoadLevel)는
//    호출자가 하고(마스터만 실제로 호출, 나머지는 PUN이 룸 프로퍼티 동기화로 내부적으로
//    따라 로드함), 여기서는 PhotonNetwork.LevelLoadingProgress만 구독해서 페이드만 담당한다.
public class FadeManager : Singleton<FadeManager>
{
    [SerializeField] private Image overlay;
    [SerializeField] private float fadeDuration = 0.5f;   // 알파 0<->255 전환 시간
    [SerializeField] private float minHoldDuration = 1.5f; // 완전히 덮인 상태로 최소 대기하는 시간

    bool busy;

    protected override void Awake()
    {
        base.Awake();
        if (Instance != this) return; // 중복 인스턴스라 곧 파괴될 경우 아래 초기화는 건너뛴다.

        Color c = overlay.color;
        c.a = 0f;
        overlay.color = c;
        overlay.raycastTarget = false;
    }

    public void FadeAndLoad(string sceneName)
    {
        if (busy) return;
        StartCoroutine(FadeAndLoadRoutine(sceneName));
    }

    public void FadeOutThenPhotonLoad(Action triggerLoad)
    {
        if (busy) return;
        StartCoroutine(FadeOutThenPhotonLoadRoutine(triggerLoad));
    }

    IEnumerator FadeAndLoadRoutine(string sceneName)
    {
        busy = true;
        overlay.raycastTarget = true;
        yield return FadeRoutine(0f, 1f);

        float holdStart = Time.time;
        var op = SceneManager.LoadSceneAsync(sceneName);

        while (!op.isDone || Time.time - holdStart < minHoldDuration)
            yield return null;

        yield return FadeRoutine(1f, 0f);
        overlay.raycastTarget = false;
        busy = false;
    }

    IEnumerator FadeOutThenPhotonLoadRoutine(Action triggerLoad)
    {
        busy = true;
        overlay.raycastTarget = true;
        yield return FadeRoutine(0f, 1f);

        float holdStart = Time.time;
        triggerLoad?.Invoke();

        // LevelLoadingProgress는 로드 시작 전엔 0, 완료 후엔 1로 고정되는 값이라
        // 이걸로 완료 여부를 판단해도 안전하다(PhotonNetworkPart.cs 문서 주석 기준).
        while (PhotonNetwork.LevelLoadingProgress < 1f || Time.time - holdStart < minHoldDuration)
            yield return null;

        yield return FadeRoutine(1f, 0f);
        overlay.raycastTarget = false;
        busy = false;
    }

    IEnumerator FadeRoutine(float fromAlpha, float toAlpha)
    {
        float t = 0f;
        Color c = overlay.color;

        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            c.a = Mathf.Lerp(fromAlpha, toAlpha, t / fadeDuration);
            overlay.color = c;
            yield return null;
        }

        c.a = toAlpha;
        overlay.color = c;
    }
}
