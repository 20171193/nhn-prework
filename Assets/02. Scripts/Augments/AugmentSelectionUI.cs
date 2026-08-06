using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 증강 선택 화면. Show로 열고, 카드를 고르거나 제한 시간이 끝나면 닫힌다.
// 시간 초과 시에는 남은 선택지 중 하나를 무작위로 골라준다
// - PvP라 한쪽이 안 고르고 버티면 매치가 멈추기 때문.
// panel은 이 스크립트가 붙은 오브젝트가 아니라 자식이어야 한다.
// 자기 자신을 끄면 Awake가 실행되지 않아 버튼 연결이 안 된다.
// (이 규칙 덕분에 게임오브젝트는 계속 활성 상태라 아래 OnEnable 등록도 정상 동작한다.)
//
// 한 매치에서 라운드마다 다시 열리므로, 닫을 때 카드 구독과 뒤집힌 상태를 반드시 되돌린다.
//
// GameManager가 이 화면을 SerializeField로 참조하지 않고, 반대로 여기서 자신을 등록한다.
// 이유는 IAugmentSelectionView 주석 참고.
public class AugmentSelectionUI : MonoBehaviour, IAugmentSelectionView
{
    public Canvas panel;
    public List<AugmentCardUI> cards;
    public RopeTimerUI ropeTimer;

    [Tooltip("내가 고른 뒤 상대를 기다리는 동안 띄울 오브젝트(\"상대가 증강을 고르는 중...\").\n" +
             "panel 아래에 두면 안 된다 - 카드를 닫을 때 panel.enabled를 끄므로 같이 사라진다. " +
             "panel과 형제로 두고 자체 Canvas를 갖게 하거나, 다른 Canvas 아래에 두어야 한다.")]
    public GameObject waitingForOthersPanel;

    AugmentManager manager;
    Action<AugmentData> onChosen;
    Coroutine countdown;

    private void Awake()
    {
        panel.enabled = false;
        SetWaitingForOthers(false);
    }

    private void OnEnable()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.RegisterSelectionView(this);
    }

    private void OnDisable()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.UnregisterSelectionView(this);
    }

    public void Show(AugmentManager manager, float duration, Action<AugmentData> onChosen = null)
    {
        this.manager = manager;
        this.onChosen = onChosen;

        panel.enabled = true;
        // 지난 회차에 띄운 대기 표시를 걷어내고 새로 연다.
        SetWaitingForOthers(false);

        var offers = manager.RollAugments(cards.Count);
        for (int i = 0; i < cards.Count; i++)
        {
            // 풀이 바닥나 선택지가 카드 수보다 적으면 남는 카드는 치운다.
            bool hasOffer = i < offers.Count;
            cards[i].gameObject.SetActive(hasOffer);
            if (!hasOffer) continue;

            // 뒤집기 연출 없이 앞면 그대로 띄운다.
            // Init이 앞면 표시와 슬롯별 리롤 1회 충전까지 맡는다.
            cards[i].Init(offers[i]);
            cards[i].OnClick += OnAugmentSelected;
            cards[i].OnReroll += OnCardReroll;
        }

        countdown = StartCoroutine(Countdown(duration));
    }

    // 증강 선택 구간이 끝났다. 아직 고르지 않았다면 고른 것 없이 화면만 걷어내고,
    // 이미 골라서 상대를 기다리는 중이었다면 그 대기 표시까지 같이 걷는다.
    public void Hide()
    {
        Close();
        SetWaitingForOthers(false);
        onChosen = null;
    }

    private void OnAugmentSelected(AugmentCardUI card)
    {
        var index = cards.FindIndex(c => c == card);
        Choose(index);
    }

    // 슬롯 하나만 다시 뽑는다. 슬롯당 라운드에 한 번이라 성공하든 실패하든 여기서 소모한다.
    // 남은 제한 시간은 그대로 간다 - 리롤로 시간을 벌 수 있으면 안 된다.
    private void OnCardReroll(AugmentCardUI card)
    {
        var index = cards.FindIndex(c => c == card);
        if (index < 0) return;

        var replacement = manager.RerollAt(index);
        if (replacement != null) card.SetAugment(replacement);

        card.SetRerollAvailable(false);
    }

    // 남은 시간은 로프가 양쪽에서 타들어가는 길이로 보여준다.
    // duration이 0이면 반복문을 아예 돌지 않으므로 0으로 나눌 일은 없다.
    private IEnumerator Countdown(float duration)
    {
        for (float left = duration; left > 0f; left -= Time.deltaTime)
        {
            ropeTimer.SetProgress(left / duration);
            yield return null;
        }

        ropeTimer.SetProgress(0f);
        Choose(UnityEngine.Random.Range(0, manager.Offers.Count));
    }

    private void Choose(int index)
    {
        var offers = manager.Offers;
        var chosen = index >= 0 && index < offers.Count ? offers[index] : null;

        // 콜백은 한 번만 나가야 한다. 넘기기 전에 먼저 비운다.
        var callback = onChosen;
        onChosen = null;

        manager.ChooseAugment(chosen);
        Close();

        // 내 선택은 끝났지만 단계는 상대가 고를 때까지 이어진다(GameManager가 양쪽 보고를 기다린다).
        // 그동안 빈 화면만 남으면 멈춘 것처럼 보이므로 기다리는 중이라고 알려준다.
        SetWaitingForOthers(HasOpponent());

        callback?.Invoke(chosen);
    }

    // 혼자면 기다릴 상대가 없다. 흐름 테스트 씬처럼 방 밖에서 도는 경우도 여기서 걸러진다.
    private bool HasOpponent() => GameManager.Instance != null && GameManager.Instance.HasOpponent;

    private void SetWaitingForOthers(bool waiting)
    {
        if (waitingForOthersPanel == null) return;
        if (waitingForOthersPanel.activeSelf == waiting) return;

        waitingForOthersPanel.SetActive(waiting);
    }

    // 화면을 닫고 카드 구독을 정리한다. 라운드마다 다시 열리므로 여기서 걷어내지 않으면
    // 다음 라운드에는 클릭 한 번이 여러 번으로 들어온다.
    private void Close()
    {
        if (countdown != null) StopCoroutine(countdown);
        countdown = null;

        for (int i = 0; i < cards.Count; i++)
        {
            cards[i].OnClick -= OnAugmentSelected;
            cards[i].OnReroll -= OnCardReroll;
        }

        panel.enabled = false;
    }
}
