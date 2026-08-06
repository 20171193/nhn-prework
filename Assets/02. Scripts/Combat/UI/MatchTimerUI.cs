using TMPro;
using UnityEngine;

// 매치 시계 HUD. 남은 매치 시간과 다음 증강 선택까지 남은 시간을 같이 보여준다.
//
// 시간은 GameManager가 쥐고 있고 여기서는 읽기만 한다. 마스터가 방송한 마감 시각으로
// 계산된 값이라, 두 클라이언트가 같은 숫자를 본다(GameManager 주석 참고).
//
// 씬에 미리 놓여 있고 GameManager는 나중에 생길 수도 있으므로(Bootstrap) 참조를 캐싱하지 않고
// 매 프레임 Instance를 확인한다. 대신 화면에 찍히는 문자열이 바뀔 때만 TMP에 대입한다 -
// 매 프레임 대입하면 초 단위로 같은 글자를 다시 그려도 메시가 통째로 다시 만들어진다.
public class MatchTimerUI : MonoBehaviour
{
    [Header("표시할 라벨")]
    [Tooltip("남은 매치 시간(전투 시간 기준).")]
    [SerializeField] private TMP_Text matchTimeLabel;
    [Tooltip("다음 증강 선택까지 남은 시간.")]
    [SerializeField] private TMP_Text augmentTimeLabel;

    [Header("문구")]
    [Tooltip("증강 카운트다운 앞에 붙는 말.")]
    [SerializeField] private string augmentPrefix = "다음 증강 ";
    [Tooltip("증강 선택이 떠 있는 동안 증강 라벨에 띄울 문구. 이때는 매치 시계가 멈춘다.")]
    [SerializeField] private string selectingText = "증강 선택 중";
    [Tooltip("남은 증강 선택이 없을 때(마지막 전투 구간) 증강 라벨에 띄울 문구.")]
    [SerializeField] private string noMoreSelectionText = "마지막 전투";

    // 마지막으로 찍은 문자열. 같은 값이면 TMP를 건드리지 않는다.
    private string lastMatchText;
    private string lastAugmentText;

    private void Update()
    {
        var gameManager = GameManager.Instance;

        // 매치가 시작되기 전과 끝난 뒤에는 시계를 띄울 이유가 없다.
        // 이 스크립트가 붙은 오브젝트가 아니라 라벨만 끈다 - 자기 자신을 끄면 Update가 멈춰
        // 매치가 시작돼도 다시 켜지 못한다.
        bool showing = gameManager != null
                       && gameManager.IsMatchRunning
                       && gameManager.Phase != GamePhase.MatchOver;

        SetLabelActive(matchTimeLabel, showing);
        SetLabelActive(augmentTimeLabel, showing);

        if (!showing) return;

        SetText(matchTimeLabel, FormatClock(gameManager.MatchTimeLeft), ref lastMatchText);
        SetText(augmentTimeLabel, BuildAugmentText(gameManager), ref lastAugmentText);
    }

    private string BuildAugmentText(GameManager gameManager)
    {
        if (gameManager.Phase == GamePhase.AugmentSelect) return selectingText;
        if (!gameManager.HasNextAugmentSelect) return noMoreSelectionText;

        return augmentPrefix + FormatClock(gameManager.TimeUntilAugmentSelect);
    }

    private void SetText(TMP_Text label, string text, ref string last)
    {
        if (label == null || text == last) return;

        label.text = text;
        last = text;
    }

    private static void SetLabelActive(TMP_Text label, bool active)
    {
        if (label == null || label.gameObject.activeSelf == active) return;

        label.gameObject.SetActive(active);
    }

    // 0.1초라도 남아 있으면 1초로 올린다. 0:00은 정말 다 됐을 때만 보여야
    // 남은 시간이 있는데 0으로 보이는 순간이 생기지 않는다.
    private static string FormatClock(float seconds)
    {
        int total = Mathf.CeilToInt(Mathf.Max(0f, seconds));
        return $"{total / 60}:{total % 60:00}";
    }
}
