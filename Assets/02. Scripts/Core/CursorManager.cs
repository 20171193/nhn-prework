using UnityEngine;
using UnityEngine.UI;

public enum CursorMode
{
    Default, // 로비, 게임룸 증강 선택 등 - 기본 커서
    Aim,     // 전투 중 - 에임/딜레이 상태로 표시
}

// Aim 모드 안에서의 세부 상태. Delay는 "공격 딜레이 진행 중"뿐 아니라 "투척무기 사거리 밖"처럼
// 진행률이 없는 경우도 포함한다 - 어느 쪽이든 시각적으로는 같은 Delay(빨강) 취급.
public enum CursorState
{
    Normal, // Default 모드 전용
    Aiming, // 공격/투척 가능
    Delay,  // 공격 딜레이 중이거나 사거리 밖
}

// 씬 전역 커스텀 커서. Bootstrap이 게임 시작 시 한 번 Instantiate + DontDestroyOnLoad로
// 띄워두고(GameManager와 같은 패턴), OS 커서는 숨긴 채 cursorRoot(오버레이 캔버스 자식,
// 자체 비주얼 없음)를 마우스 좌표로 계속 따라다니게 한다.
// 오브젝트를 따로 두지 않고 slider(aimCursor) 하나만으로 세 상태를 전부 표현한다 -
// state에 따라 value/색/애니메이터 bool을 한꺼번에 정한다(Normal/Aiming은 항상 value 1,
// Delay만 SetDelayFill로 진행률을 따로 받는다 - 진행률이 없는 Delay는 안 불러도 된다).
// 모드/상태 전환은 전투 쪽(PlayerAimController/PlayerWeaponFireController/ThrowableWeaponController)이
// 이 싱글턴에 값을 밀어넣는 방향으로 호출한다 - 반대로 전투 코드가 이 쪽을 구독하지 않는다.
public class CursorManager : Singleton<CursorManager>
{
    [SerializeField] private RectTransform cursorRoot; // 오버레이 캔버스 자식, 마우스 좌표를 계속 따라간다.
    [SerializeField] private Slider aimCursor;      // 세 상태 공용 커서 슬라이더
    [SerializeField] private Image aimCursorFill;   // aimCursor의 Fill 이미지 - 상태별 색상 변경 대상
    [SerializeField] private Animator cursorAnimator; // bool 파라미터 IsDelay/IsAiming - 참조는 직접 연결

    public Color normalColor = Color.blue;
    public Color aimEnableColor = Color.green;
    public Color aimDisableColor = Color.red;

    static readonly int IsDelayHash = Animator.StringToHash("IsDelay");
    static readonly int IsAimingHash = Animator.StringToHash("IsAiming");

    CursorMode mode = CursorMode.Default;
    CursorState state = CursorState.Normal;

    protected override void Awake()
    {
        base.Awake();

        if (Instance != this) return; // Singleton이 중복 인스턴스는 Destroy하므로, 그 경우는 아래 세팅을 건너뛴다.

        Cursor.visible = false;
        ApplyState();
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
        // Default로 나가면 Normal, Aim으로 들어오면 일단 공격 가능(Aiming) 상태로 시작한다.
        SetState(mode == CursorMode.Default ? CursorState.Normal : CursorState.Aiming);
    }

    public void SetState(CursorState newState)
    {
        // Aim 모드가 아닐 때 Aiming/Delay로 바뀌는 걸 막는다(전투 밖에서 잘못 호출돼도 무해하게).
        if (newState != CursorState.Normal && mode != CursorMode.Aim) return;

        // state가 같아도 항상 ApplyState를 다시 돌린다 - 예를 들어 총 쿨다운 중(Delay, fill 0.6)에
        // 바로 투척무기로 바꿨는데 사거리 밖이라 또 Delay면, enum은 똑같아도 그 0.6은 전혀 다른
        // 맥락의 값이라 그대로 남아있으면 안 된다(값 리셋을 건너뛰면 이전 값이 잘못 보임).
        state = newState;
        ApplyState();
    }

    // Delay 상태에서만 의미 있는 진행률(0=딜레이 시작 ~ 1=공격 가능 직전). 사거리 밖처럼
    // 진행률이 없는 Delay는 SetState(Delay)만 부르면 되고 이건 안 불러도 된다.
    public void SetDelayFill(float fill)
    {
        if (state != CursorState.Delay || aimCursor == null) return;

        aimCursor.value = Mathf.Clamp01(fill);
    }

    void ApplyState()
    {
        if (aimCursor == null) return;

        switch (state)
        {
            case CursorState.Normal:
                aimCursorFill.color= normalColor;
                aimCursor.value = 1f;
                SetAnimatorState(isDelay: false, isAiming: false);
                break;

            case CursorState.Aiming:
                aimCursorFill.color = aimEnableColor;
                aimCursor.value = 1f;
                SetAnimatorState(isDelay: false, isAiming: true);
                break;

            case CursorState.Delay:
                // 기본값 0 - 진행률 있는 쪽(총 쿨다운)은 SetDelayFill이 바로 이어서 덮어쓴다.
                // 진행률 없는 쪽(투척무기 사거리 밖)은 이 0이 그대로 유지된다.
                aimCursorFill.color = aimDisableColor;
                aimCursor.value = 0f;
                SetAnimatorState(isDelay: true, isAiming: false);
                break;
        }
    }


    void SetAnimatorState(bool isDelay, bool isAiming)
    {
        if (cursorAnimator == null) return;

        cursorAnimator.SetBool(IsDelayHash, isDelay);
        cursorAnimator.SetBool(IsAimingHash, isAiming);
    }
}
