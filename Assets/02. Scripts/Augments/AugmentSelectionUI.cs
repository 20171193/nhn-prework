using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
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
    public TMP_Text timerText;

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

            // 지난 라운드에 앞면으로 남아 있던 카드를 뒷면으로 되돌린 뒤 뒤집어야
            // 매 라운드 같은 연출이 나온다.
            cards[i].ResetToBack();
            cards[i].Init(offers[i]);
            cards[i].Flip();
            cards[i].OnClick += OnAugmentSelected;
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

    private IEnumerator Countdown(float duration)
    {
        for (float left = duration; left > 0f; left -= Time.deltaTime)
        {
            timerText.text = Mathf.CeilToInt(left).ToString();
            yield return null;
        }

        timerText.text = "0";
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
            cards[i].OnClick -= OnAugmentSelected;

        panel.enabled = false;
    }
}
