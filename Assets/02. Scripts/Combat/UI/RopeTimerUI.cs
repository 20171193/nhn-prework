using UnityEngine;
using UnityEngine.UI;

// 양쪽 끝에 불이 붙어 가운데로 타들어가는 로프 타이머. 남은 시간을 숫자 대신 로프 길이로 보여준다.
//
// 로프 아트가 세그먼트 한 칸짜리라 Image Type=Tiled로 반복시켜 긴 로프를 만든다.
// Tiled와 Filled는 둘 다 Image.type이라 같이 쓸 수 없다. 즉 fillAmount로는 줄일 수 없고,
// 대신 RectMask2D의 사각형을 좁혀서 잘라낸다. 마스크의 pivot이 가운데라 폭만 줄이면
// 좌우 경계가 동시에 안쪽으로 들어온다. 양끝에서 타는 연출이 rect 하나로 끝난다.
//
// 로프 Image의 폭은 절대 건드리지 않는 것이 핵심이다. 로프 rect를 직접 줄이면 타일이
// rect 기준으로 다시 깔리면서 로프 무늬가 옆으로 기어간다. 마스크만 움직이면 무늬는
// 제자리에 고정된 채 잘려나가기만 한다.
//
// 계층 구조:
//   RopeTimer          ← 이 스크립트. anchor/pivot 가운데
//   ├─ BurnMask        RectMask2D. anchor/pivot 가운데. 폭만 줄어든다
//   │  └─ Rope         Image Type=Tiled. anchor 가운데(stretch 아님), 폭 고정
//   ├─ FlameLeft       마스크 바깥에 둔다. 자식으로 넣으면 불꽃도 같이 잘린다
//   └─ FlameRight
//
// 남은 시간을 스스로 재지 않는다. 시간을 쥔 쪽이 SetProgress를 불러주기만 하면 된다.
public class RopeTimerUI : MonoBehaviour
{
    [Header("로프")]
    [Tooltip("RectMask2D가 붙은 오브젝트. 폭이 줄면서 로프를 양쪽에서 잘라낸다.")]
    [SerializeField] private RectTransform burnMask;
    [Tooltip("마스크 안의 로프. Image Type=Tiled. anchor는 stretch가 아니라 가운데여야 한다. 로프 전체 길이를 여기서 읽는다.")]
    [SerializeField] private RectTransform rope;

    [Header("불꽃")]
    [SerializeField] private RectTransform leftFlame;
    [SerializeField] private RectTransform rightFlame;
    [Tooltip("불꽃을 타는 경계보다 안쪽으로 얼마나 파고들게 할지(px). 불이 로프를 먹는 것처럼 보이게 하고, 잘린 세그먼트 단면도 가려준다.")]
    [SerializeField] private float flameInset = 8f;
    [Tooltip("로프가 처져 있는 아트에서만 쓴다. 가로 축은 남은 비율(0~1), 세로 축은 씬에 배치해둔 높이에서 얼마나 더할지(px). 곧은 로프면 0 그대로 두면 된다.")]
    [SerializeField] private AnimationCurve flameHeightOffset = AnimationCurve.Constant(0f, 1f, 0f);

    [Header("에디터 미리보기")]
    [Tooltip("플레이 없이 인스펙터에서 타는 모습을 확인하기 위한 값. 런타임에는 쓰이지 않는다.")]
    [SerializeField, Range(0f, 1f)] private float previewProgress = 1f;

    // 씬에 배치된 불꽃 높이. 이 스크립트는 x만 옮기는 게 기본이고, 세로 위치는 아트가 잡아둔
    // 값을 그대로 존중한다. 처진 로프일 때만 flameHeightOffset을 여기에 더한다.
    private float leftFlameBaseY;
    private float rightFlameBaseY;
    private bool baseYCaptured;

    private void Awake()
    {
        if (leftFlame != null) leftFlameBaseY = leftFlame.anchoredPosition.y;
        if (rightFlame != null) rightFlameBaseY = rightFlame.anchoredPosition.y;
        baseYCaptured = true;
    }

    // progress 1 = 로프가 온전한 상태, 0 = 다 타서 양쪽 불이 가운데서 만난 상태.
    public void SetProgress(float progress)
    {
        progress = Mathf.Clamp01(progress);

        if (rope == null || burnMask == null) return;

        // 로프 길이를 캐싱하지 않고 매번 읽는다. 아트가 로프 길이를 바꿔도 따로 손댈 곳이 없다.
        float fullWidth = rope.rect.width;

        // anchor 설정에 상관없이 폭을 맞춰준다. sizeDelta 직접 대입은 stretch anchor에서 어긋난다.
        burnMask.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, fullWidth * progress);

        // 남은 로프의 바깥쪽 끝. progress가 줄면 0(가운데)으로 다가온다.
        float burnEdge = fullWidth * 0.5f * progress;
        // 다 타버린 뒤에는 inset이 경계를 가운데 너머로 밀어버리므로 그때는 밀지 않는다.
        float inset = Mathf.Min(flameInset, burnEdge);
        float sag = flameHeightOffset.Evaluate(progress);

        // 불이 꺼진 뒤에도 불꽃이 남아 있으면 로프가 다 탄 게 아닌 것처럼 보인다.
        bool burning = progress > 0f;
        PlaceFlame(leftFlame, -(burnEdge - inset), leftFlameBaseY + sag, burning);
        PlaceFlame(rightFlame, burnEdge - inset, rightFlameBaseY + sag, burning);
    }

    private void PlaceFlame(RectTransform flame, float x, float y, bool burning)
    {
        if (flame == null) return;

        if (flame.gameObject.activeSelf != burning) flame.gameObject.SetActive(burning);
        if (!burning) return;

        // 에디터 미리보기(Awake 전)에서는 배치 높이를 아직 모른다. 그때는 세로를 건드리지 않는다.
        flame.anchoredPosition = new Vector2(x, baseYCaptured ? y : flame.anchoredPosition.y);
    }

#if UNITY_EDITOR
    // 아트 쪽에서 슬라이더만 움직여도 타는 모습을 바로 볼 수 있게 한다.
    private void OnValidate()
    {
        if (Application.isPlaying) return;

        SetProgress(previewProgress);
    }
#endif
}
