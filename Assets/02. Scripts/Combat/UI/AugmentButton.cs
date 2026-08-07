using System.Collections;
using Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// PlayerHUD에 존재하는 증강 UI 버튼
// 1. 비활성화(고르지 않은 경우) : lock 이미지, 상호작용 무시
// 2. 활성화 : 증강 종류에 따른 이미지, 클릭 시 일정시간 툴팁 표시
//
// 씬 구조(Group_AugmentHover_First/Second/Third):
//   Group_AugmentHover_N   ← 이 스크립트
//   ├─ IMG_BackGround      슬롯 배경. 이 스크립트는 건드리지 않는다
//   ├─ IMG_Augment         augmentIMG. 잠겨 있으면 자물쇠, 획득하면 증강 아이콘(smallIcon)
//   ├─ IMG_Hovered         highlightGO. 툴팁이 떠 있는 동안만 켠다
//   └─ (툴팁 오브젝트)      tooltipGO. 증강 이름/설명을 띄운다
//
// 클릭은 Button.onClick에 OnClickAugmentButton/OnClickTooltip을 연결해 들어온다.
// 툴팁 오브젝트가 아직 없어도(참조가 비어 있어도) 아이콘 표시는 그대로 동작한다.
//
// 증강 데이터는 PlayerHUD가 OnAugmentAcquired를 받아 SetAugment로 직접 넘겨준다.
// 이 버튼은 AugmentData만 알면 되므로 별도의 DB나 id 조회가 필요 없다.
public class AugmentButton : MonoBehaviour
{
    [Header("구성")]
    [SerializeField] private Image augmentIMG;
    [Tooltip("툴팁이 떠 있는 동안 켜둘 강조 이미지(IMG_Hovered).")]
    [SerializeField] private GameObject highlightGO;
    [Tooltip("증강 이름/설명을 띄우는 툴팁. 비워두면 툴팁 없이 아이콘만 동작한다.")]
    [SerializeField] private GameObject tooltipGO;

    [Header("툴팁 내용")]
    [SerializeField] private TextMeshProUGUI tooltipNameTXT;
    [SerializeField] private TextMeshProUGUI tooltipDescriptionTXT;

    [Header("설정")]
    [Tooltip("비워두면 씬의 IMG_Augment에 깔아둔 스프라이트를 자물쇠 이미지로 쓴다.")]
    [SerializeField] private Sprite lockedIcon;
    [Tooltip("툴팁을 켠 뒤 자동으로 닫히기까지의 시간(초). 0 이하면 직접 닫을 때까지 유지한다.")]
    [SerializeField] private float tooltipDuration = 5f;

    AugmentData augment;
    Coroutine autoCloseRoutine;
    // 툴팁이 열려 있는지. tooltipGO.activeSelf를 보지 않는 이유는 툴팁 참조가 비어 있어도
    // 강조 이미지 토글은 똑같이 돌아야 하기 때문이다.
    bool tooltipOpen;

    public bool HasAugment => augment != null;

    void Awake()
    {
        // 자물쇠 스프라이트를 인스펙터에 따로 꽂지 않았으면 씬에 이미 깔려 있는 것을 그대로 쓴다.
        // 아트가 슬롯에 자물쇠를 올려둔 상태 그대로 Init을 불러도 되게 하려는 것.
        if (lockedIcon == null && augmentIMG != null) lockedIcon = augmentIMG.sprite;

        Init();
    }

    // 초기화 : lock 이미지로
    public void Init()
    {
        augment = null;

        SetTooltipOpen(false);

        if (augmentIMG != null) augmentIMG.sprite = lockedIcon;
    }

    // 매개변수로 증강 정보받아오기(증강 풀 존재 시 id로 가져오기)
    public void SetAugment(AugmentData augment)
    {
        if (augment == null) return;

        this.augment = augment;

        // 아이콘이 비어 있는 증강이면 자물쇠 그대로 두지 않고 최소한 잠금은 풀린 것으로 보이게
        // 두되, 어떤 증강인지 아이콘으로는 알 수 없으므로 경고를 남긴다.
        if (augmentIMG != null)
        {
            if (augment.smallIcon != null)
            {
                augmentIMG.sprite = augment.smallIcon;
            }
            else
            {
                Debug.LogWarning($"{augment.augmentName}에 smallIcon이 없어 슬롯 아이콘을 채우지 못했습니다.", augment);
            }
        }

        if (tooltipNameTXT != null) tooltipNameTXT.text = augment.augmentName;
        if (tooltipDescriptionTXT != null) tooltipDescriptionTXT.text = augment.description;
    }

    // 버튼 클릭 시
    //  - on : tooltip/highlightImage on, tooltipDuration 뒤 자동 off
    //  - off : tooltip/highlightImage off
    public void OnClickAugmentButton()
    {
        if (!HasAugment) return; // 아직 못 얻은 슬롯은 보여줄 것이 없다

        SoundManager.Instance?.PlaySfxUI(SfxId.ClickNormalBTN);
        SetTooltipOpen(!tooltipOpen);
    }

    // 툴팁 이미지를 클릭한 경우 : 툴팁이 켜진 상태에서만 동작
    public void OnClickTooltip()
    {
        if (!tooltipOpen) return;

        SoundManager.Instance?.PlaySfxUI(SfxId.ClickNormalBTN);
        SetTooltipOpen(false);
    }

    void SetTooltipOpen(bool open)
    {
        tooltipOpen = open;

        if (tooltipGO != null) tooltipGO.SetActive(open);
        if (highlightGO != null) highlightGO.SetActive(open);

        // 열 때마다 타이머를 새로 건다. 닫을 때는 걸려 있던 것을 걷어낸다 -
        // 안 그러면 닫은 뒤에 지난 타이머가 깨어나 다음에 연 툴팁을 일찍 닫는다.
        if (autoCloseRoutine != null)
        {
            StopCoroutine(autoCloseRoutine);
            autoCloseRoutine = null;
        }

        if (!open || tooltipDuration <= 0f) return;

        // 비활성 오브젝트에서는 코루틴을 돌릴 수 없다(HUD 슬롯이 꺼져 있는 경우).
        // 그때는 자동 닫기 없이 열어두고, 다음 클릭으로 닫는다.
        if (!isActiveAndEnabled) return;

        autoCloseRoutine = StartCoroutine(CloseAfterDelay());
    }

    IEnumerator CloseAfterDelay()
    {
        yield return YieldCache.WaitForSeconds(tooltipDuration);

        autoCloseRoutine = null;
        SetTooltipOpen(false);
    }
}
