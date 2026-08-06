using TMPro;
using UnityEngine;

// 매치가 시작되기 전 준비 시간(GamePhase.Ready) 동안 뜨는 카운트다운 화면.
// 결과 화면(MatchResultUI)과 같은 구조다 - 전체를 덮는 패널 하나와 문구 라벨.
//
// 남은 시간은 GameManager가 쥐고 있고 여기서는 읽기만 한다. 준비 단계의 마감 시각도
// 마스터가 방송한 값이라, 두 클라이언트가 같은 숫자를 세고 같은 순간에 전투로 들어간다.
//
// 켜고 끄는 판단은 단계 하나로만 한다. "Ready면 켜고 아니면 끈다"는 규칙이라
// 어떤 순서로 단계가 바뀌어도 화면이 남아 있을 수 없다.
public class MatchStartUI : MonoBehaviour
{
    [Header("시작 카운트다운")]
    [Tooltip("카운트다운 화면 전체를 담은 오브젝트. 준비 단계에만 켠다.")]
    [SerializeField] private GameObject panel;
    [Tooltip("남은 시간을 보여줄 라벨.")]
    [SerializeField] private TMP_Text countdownLabel;

    [Header("문구")]
    [Tooltip("{0}에 남은 초가 들어간다.")]
    [SerializeField] private string countdownFormat = "{0}초 후 매치 시작";

    // 마지막으로 찍은 문자열. 같은 값이면 TMP를 건드리지 않는다.
    private string lastText;

    void Start()
    {
        if (GameManager.Instance == null)
        {
            SetPanelActive(false);
            Debug.LogError("GameManager가 없습니다. Resources/GameManager 프리팹이 있는지 확인하세요.", this);
            return;
        }

        GameManager.Instance.PhaseChanged += SyncToPhase;

        // 지금 단계에 맞춰 한 번 맞춰두고 시작한다. 켜는 길과 끄는 길이 같은 함수라
        // 어떤 단계에서 씬이 열려도 화면 상태가 어긋나지 않는다.
        SyncToPhase(GameManager.Instance.Phase);
    }

    void OnDestroy()
    {
        // GameManager는 씬을 넘어 살아남는다. 씬이 바뀌어도 죽은 UI가 남지 않도록 반드시 해제한다.
        if (GameManager.Instance != null) GameManager.Instance.PhaseChanged -= SyncToPhase;
    }

    void SyncToPhase(GamePhase phase)
    {
        SetPanelActive(phase == GamePhase.Ready);
    }

    void Update()
    {
        if (panel == null || !panel.activeSelf) return;

        var gameManager = GameManager.Instance;
        if (gameManager == null || countdownLabel == null) return;

        // 0.1초라도 남아 있으면 1초로 올린다. 3초 준비 시간이 3, 2, 1로 떨어진다.
        string text = string.Format(countdownFormat, Mathf.CeilToInt(gameManager.PhaseTimeLeft));
        if (text == lastText) return;

        countdownLabel.text = text;
        lastText = text;
    }

    void SetPanelActive(bool active)
    {
        // panel을 따로 두는 이유: 이 스크립트가 붙은 오브젝트를 끄면 Start도 OnDestroy도
        // 정상적으로 돌지 않아 구독이 꼬인다. 꺼지는 것은 항상 자식 쪽이어야 한다.
        if (panel != null && panel.activeSelf != active) panel.SetActive(active);

        // 다음에 다시 띄울 때 지난 숫자에서 시작하지 않도록 비워둔다.
        if (!active) lastText = null;
    }
}
