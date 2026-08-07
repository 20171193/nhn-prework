using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

// 모든 사운드 재생을 담당하는 싱글턴. FadeManager와 같은 패턴(Bootstrap 자동 생성,
// DontDestroyOnLoad)이라 씬이 바뀌어도 BGM이 끊기지 않고 크로스페이드로 이어진다.
//
// BGM: AudioSource 2개를 번갈아 쓰며 짧게 크로스페이드(겹쳐 재생)한다.
// SFX: 위치를 받아 3D(spatialBlend=1)로 재생 - 카메라의 AudioListener 기준 거리 감쇠가
// 엔진에서 자동 계산된다. 동시에 여러 개 재생될 수 있어 AudioSource를 풀링해서 돌려쓴다.
public class SoundManager : Singleton<SoundManager>
{
    [Header("믹서")]
    [SerializeField] private AudioMixerGroup bgmMixerGroup;
    [SerializeField] private AudioMixerGroup sfxMixerGroup;
    [SerializeField] private AudioMixerGroup ambMixerGroup;

    [Header("BGM 크로스페이드")]
    [SerializeField] private float bgmCrossfadeDuration = 0.7f;

    [Header("SFX 풀")]
    [SerializeField] private int sfxPoolSize = 8;

    AudioSource bgmSourceA;
    AudioSource bgmSourceB;
    AudioSource activeBgmSource; // 현재 메인으로 재생 중인 쪽
    int currentBgmId = -1;
    Coroutine bgmCrossfadeRoutine;

    readonly List<AudioSource> sfxPool = new List<AudioSource>();

    AudioSource ambSource;
    Coroutine ambRoutine;

    AudioSource uiOneShotSource; // 버튼 클릭 등 - 위치 무관, 겹쳐 재생돼도 서로 안 끊김(PlayOneShot)
    AudioSource uiLoopSource;    // 타이머 진행음 등 - 위치 무관, 켜고 끄는 루프

    // 다른 스크립트(발소리처럼 특정 오브젝트에 종속된 사운드)가 직접 AudioSource를 만들 때
    // 같은 믹서 그룹으로 라우팅하기 위해 노출한다.
    public AudioMixerGroup SfxMixerGroup => sfxMixerGroup;

    protected override void Awake()
    {
        base.Awake();
        if (Instance != this) return; // 중복 인스턴스라 곧 파괴될 경우 아래 초기화는 건너뛴다.

        bgmSourceA = CreateBgmSource();
        bgmSourceB = CreateBgmSource();
        activeBgmSource = bgmSourceA;

        for (int i = 0; i < sfxPoolSize; i++)
            sfxPool.Add(CreateSfxSource());

        ambSource = CreateAmbSource();

        uiOneShotSource = CreateUiSource("UiOneShotSource");
        uiLoopSource = CreateUiSource("UiLoopSource");
        uiLoopSource.loop = true;
    }

    AudioSource CreateBgmSource()
    {
        var go = new GameObject("BgmSource");
        go.transform.SetParent(transform);

        var source = go.AddComponent<AudioSource>();
        source.outputAudioMixerGroup = bgmMixerGroup;
        source.loop = true;
        source.playOnAwake = false;
        source.spatialBlend = 0f; // BGM은 항상 2D(위치 무관, 항상 같은 크기로 들림)
        source.volume = 0f;
        return source;
    }

    AudioSource CreateAmbSource()
    {
        var go = new GameObject("AmbSource");
        go.transform.SetParent(transform);

        var source = go.AddComponent<AudioSource>();
        source.outputAudioMixerGroup = ambMixerGroup;
        source.loop = false; // 한 번 재생 후 대기하다 다시 재생하는 방식이라 자체 loop는 안 쓴다.
        source.playOnAwake = false;
        source.spatialBlend = 0f; // 화면 전체에 균일하게 들려야 하므로 위치 무관 2D
        return source;
    }

    AudioSource CreateUiSource(string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(transform);

        var source = go.AddComponent<AudioSource>();
        source.outputAudioMixerGroup = sfxMixerGroup;
        source.playOnAwake = false;
        source.spatialBlend = 0f; // UI 사운드는 항상 위치 무관 2D
        return source;
    }

    AudioSource CreateSfxSource()
    {
        var go = new GameObject("SfxSource");
        go.transform.SetParent(transform);

        var source = go.AddComponent<AudioSource>();
        source.outputAudioMixerGroup = sfxMixerGroup;
        source.playOnAwake = false;
        source.spatialBlend = 1f; // 위치 기반 3D - 거리 감쇠 적용
        source.rolloffMode = AudioRolloffMode.Linear;
        return source;
    }

    public void PlayBgm(BgmId id)
    {
        int intId = (int)id;
        if (intId == currentBgmId) return; // 이미 재생 중인 BGM이면 무시

        if (BgmDatabase.Instance == null)
        {
            Debug.LogWarning("BgmDatabase가 로드되지 않았습니다(Resources에 에셋이 없을 수 있음).", this);
            return;
        }

        if (!BgmDatabase.Instance.TryGet(intId, out var data) || data.clip == null)
        {
            Debug.LogWarning($"BgmId {id}에 해당하는 BGM을 찾지 못했습니다.", this);
            return;
        }

        currentBgmId = intId;

        if (bgmCrossfadeRoutine != null) StopCoroutine(bgmCrossfadeRoutine);
        bgmCrossfadeRoutine = StartCoroutine(CrossfadeBgmRoutine(data));
    }

    IEnumerator CrossfadeBgmRoutine(BgmData data)
    {
        var fadeOutSource = activeBgmSource;
        var fadeInSource = activeBgmSource == bgmSourceA ? bgmSourceB : bgmSourceA;

        fadeInSource.clip = data.clip;
        fadeInSource.loop = data.loop;
        fadeInSource.volume = 0f;
        fadeInSource.Play();

        float targetVolume = data.volume;
        float startVolume = fadeOutSource.volume;
        float t = 0f;

        while (t < bgmCrossfadeDuration)
        {
            t += Time.deltaTime;
            float ratio = t / bgmCrossfadeDuration;
            fadeInSource.volume = Mathf.Lerp(0f, targetVolume, ratio);
            fadeOutSource.volume = Mathf.Lerp(startVolume, 0f, ratio);
            yield return null;
        }

        fadeInSource.volume = targetVolume;
        fadeOutSource.volume = 0f;
        fadeOutSource.Stop();

        activeBgmSource = fadeInSource;
        bgmCrossfadeRoutine = null;
    }

    // 위치가 의미 없는 UI 사운드(버튼 클릭 등) 전용. PlayOneShot이라 겹쳐 눌러도 서로 끊지 않는다.
    public void PlaySfxUI(SfxId id)
    {
        int intId = (int)id;
        if (SfxDatabase.Instance == null)
        {
            Debug.LogWarning("SfxDatabase가 로드되지 않았습니다(Resources에 에셋이 없을 수 있음).", this);
            return;
        }

        if (!SfxDatabase.Instance.TryGet(intId, out var data) || data.clip == null)
        {
            Debug.LogWarning($"SfxId {id}에 해당하는 효과음을 찾지 못했습니다.", this);
            return;
        }

        uiOneShotSource.PlayOneShot(data.clip, data.volume);
    }

    // 지속 시간이 정해지지 않은 UI 루프 사운드(타이머 진행음 등). 켜고 끄는 건 호출자 책임.
    public void PlayLoopingSfx(SfxId id)
    {
        int intId = (int)id;
        if (SfxDatabase.Instance == null)
        {
            Debug.LogWarning("SfxDatabase가 로드되지 않았습니다(Resources에 에셋이 없을 수 있음).", this);
            return;
        }

        if (!SfxDatabase.Instance.TryGet(intId, out var data) || data.clip == null)
        {
            Debug.LogWarning($"SfxId {id}에 해당하는 효과음을 찾지 못했습니다.", this);
            return;
        }

        uiLoopSource.clip = data.clip;
        uiLoopSource.volume = data.volume;
        uiLoopSource.Play();
    }

    public void StopLoopingSfx()
    {
        uiLoopSource.Stop();
    }

    public void PlaySfx(SfxId id, Vector3 position)
    {
        int intId = (int)id;
        if (SfxDatabase.Instance == null)
        {
            Debug.LogWarning("SfxDatabase가 로드되지 않았습니다(Resources에 에셋이 없을 수 있음).", this);
            return;
        }

        if (!SfxDatabase.Instance.TryGet(intId, out var data) || data.clip == null)
        {
            Debug.LogWarning($"SfxId {id}에 해당하는 효과음을 찾지 못했습니다.", this);
            return;
        }

        var source = GetAvailableSfxSource();
        if (source == null) return; // 풀이 전부 사용 중이면 그냥 생략 - 우선순위 낮은 효과음이라 끊겨도 무방

        source.transform.position = position;
        source.clip = data.clip;
        source.volume = data.volume;
        source.minDistance = data.minDistance;
        source.maxDistance = data.maxDistance;
        source.Play();
    }

    public void StartAmbLoop(AmbId id)
    {
        int intId = (int)id;
        if (AmbDatabase.Instance == null)
        {
            Debug.LogWarning("AmbDatabase가 로드되지 않았습니다(Resources에 에셋이 없을 수 있음).", this);
            return;
        }

        if (!AmbDatabase.Instance.TryGet(intId, out var data) || data.clip == null)
        {
            Debug.LogWarning($"AmbId {id}에 해당하는 환경음을 찾지 못했습니다.", this);
            return;
        }

        StopAmbLoop();
        ambRoutine = StartCoroutine(AmbLoopRoutine(data));
    }

    public void StopAmbLoop()
    {
        if (ambRoutine != null)
        {
            StopCoroutine(ambRoutine);
            ambRoutine = null;
        }

        ambSource.Stop();
    }

    IEnumerator AmbLoopRoutine(AmbData data)
    {
        while (true)
        {
            ambSource.clip = data.clip;
            ambSource.volume = data.volume;
            ambSource.Play();
            yield return new WaitForSeconds(data.interval);
        }
    }

    AudioSource GetAvailableSfxSource()
    {
        foreach (var source in sfxPool)
        {
            if (!source.isPlaying) return source;
        }

        return null;
    }
}
