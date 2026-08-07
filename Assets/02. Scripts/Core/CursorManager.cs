using UnityEngine;
using UnityEngine.UI;

public enum CursorMode
{
    Default, // 로비, 게임룸 증강 선택 등 - 기본 커서
    Aim,     // 전투 중 - 에임 이미지 + 공격 딜레이 fill
}

// 씬 전역 커스텀 커서. Bootstrap이 게임 시작 시 한 번 Instantiate + DontDestroyOnLoad로
// 띄워두고(GameManager와 같은 패턴), OS 커서는 숨긴 채 cursorRoot(오버레이 캔버스 자식,
// 자체 비주얼 없음)를 마우스 좌표로 계속 따라다니게 한다. 모드별 비주얼은 그 자식으로
// 미리 배치된 normalCursor/aimCursor 두 오브젝트를 active 토글로 전환한다
// (ThrowableObject의 model/triggerEffect와 같은 패턴).
// 모드 전환/공격 딜레이 fill 갱신은 전투 쪽(PlayerAimController/PlayerWeaponFireController)이
// 이 싱글턴에 값을 밀어넣는 방향으로 호출한다 - 반대로 전투 코드가 이 쪽을 구독하지 않는다.
public class CursorManager : Singleton<CursorManager>
{
    [SerializeField] private RectTransform cursorRoot; // 오버레이 캔버스 자식, 마우스 좌표를 계속 따라간다.
    [SerializeField] private GameObject normalCursor; // img_normal - 기본 모드 비주얼
    [SerializeField] private Slider aimCursor;         // slider_aim - 에임 모드 비주얼 겸 공격 딜레이 fill
    [SerializeField] private Image aimCursorFill;     // slider_aim의 Fill 이미지 - 공격 가능 여부에 따라 색상 변경
    
    public Color aimDisableColor = Color.red;
    public Color aimEnableColor = Color.green;

    CursorMode mode = CursorMode.Default;

    

    protected override void Awake()
    {
        base.Awake();

        if (Instance != this) return; // Singleton이 중복 인스턴스는 Destroy하므로, 그 경우는 아래 세팅을 건너뛴다.

        Cursor.visible = false;
        ApplyMode();
    }

    void Update()
    {
        if (cursorRoot != null)
            cursorRoot.position = Input.mousePosition;
    }

    public void SetMode(CursorMode newMode)
    {
        if (mode == newMode) return;

        mode = newMode;
        ApplyMode();
    }

    // fill: 0(공격 직후, 딜레이 시작) ~ 1(공격 가능). Aim 모드가 아닐 때 호출돼도 무해하다.
    public void SetAttackDelayFill(float fill)
    {
        if (aimCursor != null)
        {
            float clampedFill = Mathf.Clamp01(fill);    
            aimCursor.value = Mathf.Clamp01(clampedFill);
            
            aimCursorFill.color = clampedFill >= 0.99f ? aimEnableColor : aimDisableColor; 
        }
    }

    void ApplyMode()
    {
        bool aiming = mode == CursorMode.Aim;

        if (normalCursor != null) normalCursor.SetActive(!aiming);
        if (aimCursor != null) aimCursor.gameObject.SetActive(aiming);
    }
}
