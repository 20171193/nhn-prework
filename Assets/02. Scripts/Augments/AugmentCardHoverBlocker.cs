using UnityEngine;
using UnityEngine.EventSystems;

// 카드 안에 있지만 "카드에 마우스를 올린 것"으로 치면 안 되는 영역에 붙인다.
// 지금은 리롤 버튼이 여기에 해당한다.
//
// 카드 호버 판정은 AugmentCardUI 루트에서 받는데, 유니티는 자식 위에 포인터가
// 있어도 부모까지 enter를 올려보낸다. 게다가 카드 앞면에서 버튼으로 옮겨갈 때는
// 두 오브젝트의 공통 부모가 카드 루트라서 루트에는 exit도 enter도 오지 않는다.
// 그래서 버튼 쪽에서 직접 "지금은 카드 호버가 아니다"라고 알려줘야 한다.
public class AugmentCardHoverBlocker : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private AugmentCardUI card;

    // 인스펙터에서 컴포넌트를 붙이는 순간 부모 카드를 자동으로 찾아 넣는다.
    private void Reset()
    {
        card = GetComponentInParent<AugmentCardUI>();
    }

    private void Awake()
    {
        if (card == null) card = GetComponentInParent<AugmentCardUI>();
    }

    // 버튼이 꺼질 때는 exit이 오지 않는다. 눌러둔 상태로 남으면 카드 호버가
    // 영영 안 켜지므로 여기서 풀어준다.
    private void OnDisable()
    {
        if (card != null) card.SetHoverBlocked(false);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (card != null) card.SetHoverBlocked(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (card != null) card.SetHoverBlocked(false);
    }
}
