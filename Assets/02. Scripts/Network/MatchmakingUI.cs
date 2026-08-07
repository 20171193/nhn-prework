using TMPro;
using UnityEngine;
using UnityEngine.UI;

// MatchmakingManager의 상태를 버튼/라벨에 반영한다.
// 매칭 로직은 전부 매니저에 있고 여기서는 표시와 입력만 다룬다.
public class MatchmakingUI : MonoBehaviour
{
    [SerializeField] MatchmakingManager matchmaking;
    [SerializeField] Button findMatchButton;
    [SerializeField] Button cancelButton;
    [SerializeField] TMP_Text statusLabel;

    void Awake()
    {
        if (matchmaking == null)
            matchmaking = FindObjectOfType<MatchmakingManager>();
    }

    void OnEnable()
    {
        if (matchmaking == null || findMatchButton == null || cancelButton == null)
        {
            Debug.LogError("MatchmakingUI: 인스펙터 참조가 비어 있습니다.");
            enabled = false;
            return;
        }

        matchmaking.StateChanged += OnStateChanged;
        matchmaking.StatusChanged += OnStatusChanged;
        matchmaking.CountdownTick += OnCountdownTick;

        findMatchButton.onClick.AddListener(matchmaking.FindMatch);
        cancelButton.onClick.AddListener(matchmaking.CancelSearch);
        findMatchButton.onClick.AddListener(PlayClickSound);
        cancelButton.onClick.AddListener(PlayClickSound);

        OnStateChanged(matchmaking.State);
        OnStatusChanged("대기 중");
    }

    void OnDisable()
    {
        if (matchmaking == null) return;

        matchmaking.StateChanged -= OnStateChanged;
        matchmaking.StatusChanged -= OnStatusChanged;
        matchmaking.CountdownTick -= OnCountdownTick;

        if (findMatchButton != null) findMatchButton.onClick.RemoveListener(matchmaking.FindMatch);
        if (cancelButton != null) cancelButton.onClick.RemoveListener(matchmaking.CancelSearch);
        if (findMatchButton != null) findMatchButton.onClick.RemoveListener(PlayClickSound);
        if (cancelButton != null) cancelButton.onClick.RemoveListener(PlayClickSound);
    }

    static void PlayClickSound() => SoundManager.Instance?.PlaySfxUI(SfxId.ClickNormalBTN);

    void OnStateChanged(MatchmakingState state)
    {
        bool canSearch = state == MatchmakingState.Idle || state == MatchmakingState.Failed;

        // 매칭이 성사된 뒤에는 취소를 막는다. 한쪽만 씬을 로드하는 상황을 만들지 않기 위해서다.
        bool canCancel = state == MatchmakingState.Connecting || state == MatchmakingState.Searching;

        findMatchButton.gameObject.SetActive(canSearch);
        cancelButton.gameObject.SetActive(canCancel);
    }

    void OnStatusChanged(string status)
    {
        if (statusLabel != null)
            statusLabel.text = status;
    }

    void OnCountdownTick(int secondsLeft)
    {
        if (statusLabel == null || secondsLeft <= 0) return;

        statusLabel.text = $"상대를 찾았습니다! {secondsLeft}...";
    }
}
