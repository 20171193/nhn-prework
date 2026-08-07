using System;
using UnityEngine;
using UnityEngine.UI;

// LobbyPlayerSetup이 DB 항목마다 하나씩 Instantiate해서 만드는 선택지 버튼.
// id/아이콘은 Init()으로 코드가 주입한다(인스펙터 수동 입력 아님).
[RequireComponent(typeof(Button))]
public class SelectableOption : MonoBehaviour
{
    [SerializeField] private int id;
    [SerializeField] private Image icon;

    Action<SelectableOption> onClick;

    public int Id => id;
    public Sprite Icon => icon != null ? icon.sprite : null;

    void Awake()
    {
        GetComponent<Button>().onClick.AddListener(() => onClick?.Invoke(this));
    }

    public void Init(int id, Sprite icon)
    {
        this.id = id;
        SetIcon(icon);
    }

    public void SetIcon(Sprite sprite)
    {
        if (icon != null) icon.sprite = sprite;
    }

    // += 대신 덮어쓰기 - 이 옵션은 로비 재방문마다 LobbyPlayerSetup.Bind()에서 다시 호출되는데,
    // event(+=)였다면 방문할수록 핸들러가 누적되어 클릭 한 번에 여러 번 반응하는 버그가 생긴다.
    public void SetClickHandler(Action<SelectableOption> handler)
    {
        onClick = handler;
    }
}
