using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 증강 선택 화면. Show로 열고, 카드를 고르거나 제한 시간이 끝나면 닫힌다.
// 시간 초과 시에는 남은 선택지 중 하나를 무작위로 골라준다
// - PvP라 한쪽이 안 고르고 버티면 매치가 멈추기 때문.
// panel은 이 스크립트가 붙은 오브젝트가 아니라 자식이어야 한다.
// 자기 자신을 끄면 Awake가 실행되지 않아 버튼 연결이 안 된다.
public class AugmentSelectionUI : MonoBehaviour
{
    public Canvas panel;
    public List<AugmentCardUI> cards;
    public TMP_Text timerText;
    public float duration = 15f;

    AugmentManager manager;
    System.Action<AugmentData> onChosen;
    Coroutine countdown;

    private void Awake()
    {
        panel.enabled = false;
    }

    public void Show(AugmentManager manager, System.Action<AugmentData> onChosen = null)
    {
        this.manager = manager;
        this.onChosen = onChosen;

        panel.enabled = true;
        
        var offers = manager.RollAugments(cards.Count);
        for (int i = 0; i < cards.Count; i++)
        {
            cards[i].Init(offers[i]);
            cards[i].Flip();
            cards[i].OnClick += OnAugmentSelected;
        }

        countdown = StartCoroutine(Countdown());
    }

    private void OnAugmentSelected(AugmentCardUI card)
    {
        var index = cards.FindIndex(c => c == card);
        Choose(index);
    }

    private IEnumerator Countdown()
    {
        for (float left = duration; left > 0f; left -= Time.deltaTime)
        {
            timerText.text = Mathf.CeilToInt(left).ToString();
            yield return null;
        }

        timerText.text = "0";
        Choose(Random.Range(0, manager.Offers.Count));
    }

    private void Choose(int index)
    {
        var offers = manager.Offers;
        var chosen = index < offers.Count ? offers[index] : null;

        if (countdown != null) StopCoroutine(countdown);
        countdown = null;

        manager.ChooseAugment(chosen);
        panel.enabled = false;
        onChosen?.Invoke(chosen);
    }
}
