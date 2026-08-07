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

    [Tooltip("Screen Space - Camera 캔버스를 올려둘 정렬 레이어. 불꽃 파티클도 같은 레이어에 두고 " +
             "Order in Layer를 캔버스의 Sort Order보다 크게 잡아야 UI 위로 올라온다.\n" +
             "비워두면 캔버스의 정렬 레이어를 건드리지 않는다.")]
    [SerializeField] private string sortingLayerName = "UI";

    AugmentManager manager;
    Action<AugmentData> onChosen;
    Coroutine countdown;
    // 없는 정렬 레이어를 가리키고 있을 때 경고를 매번 쏟지 않도록(화면은 라운드마다 열린다).
    bool sortingLayerWarned;

    private void Awake()
    {
        panel.enabled = false;
        SetWaitingForOthers(false);
        AssignRenderCamera();
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

        // 이 화면은 GameManager가 만들어 씬을 넘어 살아남는다. 전투 씬이 새로 로드되면
        // Awake 때 잡아둔 카메라는 이미 파괴돼 있으므로, 열 때마다 지금 씬의 것으로 다시 맞춘다.
        AssignRenderCamera();

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
        SoundManager.Instance?.PlayLoopingSfx(SfxId.AugmentSelectTimer);

        Debug.Log($"[증강로그] Show 호출 - duration={duration:F2}, offers={offers.Count}, " +
                  $"Time={Time.time:F2}, PhotonTime={(Photon.Pun.PhotonNetwork.InRoom ? Photon.Pun.PhotonNetwork.Time : -1):F2}");
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

        Debug.Log($"[증강로그] Choose 호출 - index={index}, chosen={(chosen != null ? chosen.name : "null")}, " +
                  $"onChosen콜백있음={onChosen != null}, Time={Time.time:F2}");

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

    // Screen Space - Camera 캔버스에 렌더 카메라를 꽂아준다.
    //
    // 이 모드에서만 캔버스가 카메라의 렌더 패스 안에서 그려지고, 그래야 캔버스 소속이 아닌
    // 렌더러(로프 타이머의 불꽃 파티클 등)를 UI와 같이 화면에 올릴 수 있다.
    // Overlay는 카메라가 다 그린 뒤에 따로 합성하는 방식이라 파티클이 낄 자리가 없다.
    //
    // 이 값을 인스펙터에서 미리 꽂아둘 수 없는 이유: 프리팹은 씬 오브젝트를 참조로 들 수 없다.
    // 비워두면(m_Camera: 0) 유니티가 Overlay처럼 그려버려서, 파티클은 카메라가 보지 않는
    // 좌표(캔버스 rect는 화면 픽셀 크기의 월드 공간에 있다)에 그려진 채 게임 뷰에서 사라진다.
    private void AssignRenderCamera()
    {
        var renderCamera = Camera.main;
        bool sortingLayerReady = CheckSortingLayer();

        // panel과 대기 표시가 각자 Canvas를 갖는 구조라 한 번에 훑는다(꺼져 있는 것도 포함).
        foreach (var canvas in GetComponentsInChildren<Canvas>(true))
        {
            if (renderCamera == null)
            {
                Debug.LogWarning($"MainCamera 태그가 붙은 카메라를 찾지 못했습니다. " +
                                 $"{canvas.name}이 Overlay처럼 그려져 파티클이 게임 뷰에 보이지 않습니다.", canvas);
                continue;
            }

            // 카메라는 이미 같은 것이면 다시 꽂지 않는다. 정렬 레이어는 그와 별개로 매번 확인한다 -
            // 카메라만 보고 건너뛰면 두 번째 라운드부터는 레이어를 영영 못 맞춘다.
            if (canvas.worldCamera != renderCamera) canvas.worldCamera = renderCamera;

            if (sortingLayerReady) canvas.sortingLayerName = sortingLayerName;
        }
    }

    // 정렬 레이어는 이 캔버스가 카메라가 그리는 다른 렌더러들 사이 어디에 설지를 정한다.
    // 없는 이름을 넣으면 유니티가 조용히 무시하므로(오류도 안 난다) 먼저 있는지 확인하고,
    // 없으면 왜 안 먹었는지 한 번 알려준다.
    private bool CheckSortingLayer()
    {
        if (string.IsNullOrEmpty(sortingLayerName)) return false;

        foreach (var layer in SortingLayer.layers)
        {
            if (layer.name == sortingLayerName) return true;
        }

        if (!sortingLayerWarned)
        {
            sortingLayerWarned = true;
            Debug.LogWarning($"'{sortingLayerName}' 정렬 레이어가 없어 캔버스 정렬 레이어를 그대로 둡니다. " +
                             "Project Settings > Tags and Layers > Sorting Layers에 추가하세요.", this);
        }

        return false;
    }

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
        Debug.Log($"[증강로그] Close 호출 - 아직카운트다운중이었음={countdown != null} " +
                  $"(true면 Choose가 아니라 GameManager.Hide()로 외부에서 강제로 닫힌 것), Time={Time.time:F2}");

        if (countdown != null) StopCoroutine(countdown);
        countdown = null;
        SoundManager.Instance?.StopLoopingSfx();

        // 로프를 다 탄 상태로 만들어 불꽃 오브젝트까지 꺼둔다(RopeTimerUI.PlaceFlame이 SetActive로 끈다).
        // panel.enabled만 끄면 캔버스 그래픽만 사라지고, 캔버스 소속이 아닌 불꽃 파티클은
        // 계속 화면에 남아 탄다. 시간 초과로 닫히는 경우와 달리 카드를 일찍 고르면
        // progress가 0에 닿지 않은 채로 닫히기 때문에 여기서 직접 정리해준다.
        if (ropeTimer != null) ropeTimer.SetProgress(0f);

        for (int i = 0; i < cards.Count; i++)
        {
            cards[i].OnClick -= OnAugmentSelected;
            cards[i].OnReroll -= OnCardReroll;
        }

        panel.enabled = false;
    }
}
