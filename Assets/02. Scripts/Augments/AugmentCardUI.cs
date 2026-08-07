using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class AugmentCardUI : MonoBehaviour, IPointerDownHandler, IPointerEnterHandler, IPointerExitHandler
{
    public RectTransform front;
    public RectTransform back;

    // 마우스를 올렸을 때만 켜지는 테두리. 카드 루트 아래 HoverEffect 오브젝트.
    public GameObject hoverEffect;

    [SerializeField] private AugmentData augmentData;
    public TextMeshProUGUI augmentNameText;
    public TextMeshProUGUI augmentDescriptionText;
    public Image iconImage;

    // 슬롯마다 라운드당 한 번 쓸 수 있는 리롤 버튼.
    public Button rerollButton;

    public float flipDuration = 0.4f;

    public Action<AugmentCardUI> OnClick;
    public Action<AugmentCardUI> OnReroll;

    private Coroutine flipping;
    private bool showingFront;

    // 포인터가 카드 안에 있는지와, 카드 안에서도 호버로 치지 않는 영역
    // (리롤 버튼 등) 위에 있는지를 따로 들고 있다가 둘을 합쳐서 판단한다.
    private bool pointerInside;
    private bool hoverBlocked;
    private bool rerollAvailable;

    private void Awake()
    {
        // 카드는 처음부터 앞면으로 보여준다. 뒤집기 연출은 쓰지 않기로 했다.
        ShowFront(true);
        SetHovered(false);

        if (rerollButton != null) rerollButton.onClick.AddListener(HandleRerollClicked);
    }

    // 카드가 꺼질 때는 OnPointerExit이 오지 않는다. 마우스를 올린 채로 라운드가
    // 끝나면 다음 라운드에 테두리가 켜진 상태로 돌아오므로 여기서 직접 끈다.
    private void OnDisable()
    {
        SetHovered(false);
    }

    // 라운드가 시작될 때 카드 하나를 처음 상태로 세팅한다. 리롤 횟수도 여기서 채운다.
    public void Init(AugmentData data)
    {
        SetAugment(data);

        ShowFront(true);
        SetHovered(false);
        SetRerollAvailable(true);
    }

    // 표시 내용만 갈아끼운다. 리롤로 증강만 바뀔 때 쓰므로 리롤 횟수는 건드리지 않는다.
    public void SetAugment(AugmentData data)
    {
        augmentData = data;
        augmentNameText.text = data.augmentName;
        augmentDescriptionText.text = data.description;
        SetIcon(data.bigIcon);
    }

    // 카드에는 큰 아이콘(bigIcon)을 쓴다. HUD의 획득 슬롯은 같은 증강의 smallIcon을 쓴다.
    // 아이콘이 없는 증강이면 이미지를 꺼둔다 - 그냥 두면 리롤 직전 카드의 그림이 그대로 남아
    // 다른 증강의 아이콘을 달고 있는 것처럼 보인다.
    // 선택지는 라운드마다 다시 뽑히므로 여기서는 경고를 남기지 않는다(로그가 매번 쌓인다).
    private void SetIcon(Sprite icon)
    {
        if (iconImage == null) return;

        iconImage.sprite = icon;
        iconImage.enabled = icon != null;
    }

    public void SetRerollAvailable(bool available)
    {
        rerollAvailable = available;

        // Transition이 ColorTint라 다 쓰면 알아서 흐려진다.
        if (rerollButton != null) rerollButton.interactable = available;
    }

    // 버튼이 Selectable이라 포인터 이벤트가 카드 루트까지 올라가지 않는다.
    // 덕분에 리롤을 눌러도 카드가 선택되지는 않는다.
    private void HandleRerollClicked()
    {
        if (!rerollAvailable) return;
        SoundManager.Instance?.PlaySfxUI(SfxId.ClickNormalBTN);
        OnReroll?.Invoke(this);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        SoundManager.Instance?.PlaySfxUI(SfxId.ClickAugmentSelectBTN);
        OnClick?.Invoke(this);
    }

    // 카드 자식이면 무엇을 가리키든 이 이벤트가 올라온다. 리롤 버튼도 자식이라
    // 여기까지 enter가 올라오므로, 버튼 쪽에서 SetHoverBlocked로 따로 눌러준다.
    public void OnPointerEnter(PointerEventData eventData)
    {
        pointerInside = true;
        ApplyHover();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        pointerInside = false;
        ApplyHover();
    }

    // 카드 안에 있지만 호버로 치지 않을 영역이 알려준다.
    // AugmentCardHoverBlocker 참고.
    public void SetHoverBlocked(bool blocked)
    {
        hoverBlocked = blocked;
        ApplyHover();
    }

    private void SetHovered(bool hovered)
    {
        pointerInside = hovered;
        hoverBlocked = false;
        ApplyHover();
    }

    private void ApplyHover()
    {
        if (hoverEffect != null) hoverEffect.SetActive(pointerInside && !hoverBlocked);
    }

    // ── 뒤집기 연출 (현재 미사용) ──────────────────────────────────────
    // 카드를 앞면으로 바로 띄우기로 하면서 호출부를 걷어냈다. 나중에 연출을
    // 다시 넣을 수 있게 코드는 남겨둔다. 되살리려면 AugmentSelectionUI.Show에서
    // Init 앞뒤로 ResetToBack() / Flip()을 다시 불러주면 된다.
    // 인스펙터 컨텍스트 메뉴의 Flip으로 지금도 동작을 확인할 수 있다.

    [ContextMenu("Flip")]
    public void Flip()
    {
        if (flipping != null) StopCoroutine(flipping);
        flipping = StartCoroutine(FlipRoutine(!showingFront));
    }

    // 연출 없이 뒷면으로 되돌린다. 라운드가 새로 시작될 때 카드를 다시 뒤집어 보여주려면
    // 지난 라운드에 앞면으로 남아 있던 상태를 먼저 지워야 한다.
    public void ResetToBack()
    {
        if (flipping != null) StopCoroutine(flipping);
        flipping = null;

        SetAngle(0f);
        ShowFront(false);
    }

    private IEnumerator FlipRoutine(bool toFront)
    {
        yield return Rotate(0f, 90f);

        ShowFront(toFront);
        yield return Rotate(-90f, 0f);

        flipping = null;
    }

    private IEnumerator Rotate(float fromAngle, float toAngle)
    {
        float half = flipDuration * 0.5f;

        for (float t = 0f; t < half; t += Time.deltaTime)
        {
            SetAngle(Mathf.Lerp(fromAngle, toAngle, t / half));
            yield return null;
        }

        SetAngle(toAngle);
    }

    private void SetAngle(float yAngle)
    {
        transform.localRotation = Quaternion.Euler(0f, yAngle, 0f);
    }

    private void ShowFront(bool showFront)
    {
        showingFront = showFront;
        front.gameObject.SetActive(showFront);
        back.gameObject.SetActive(!showFront);
    }
}
