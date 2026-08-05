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

    AugmentManager manager;
    Action<AugmentData> onChosen;
    Coroutine countdown;

    private void Awake()
    {
        panel.enabled = false;
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

    // 매치가 중단된 경우. 고른 것 없이 화면만 걷어낸다.
    public void Hide()
    {
        Close();
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

        callback?.Invoke(chosen);
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
