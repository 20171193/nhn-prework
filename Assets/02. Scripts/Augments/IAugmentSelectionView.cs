using System;

// 증강 선택 화면이 GameManager에 자신을 노출하는 창구.
//
// GameManager는 매치 흐름만 알면 되고, 그 화면이 어떤 UI로 그려지는지는 몰라도 된다.
// 참조를 GameManager -> UI 방향의 SerializeField로 두지 않고 인터페이스 + 등록 방식으로
// 뒤집은 이유는 두 가지다.
//   1. GameManager는 씬을 넘어 살아남지만 UI는 씬과 함께 사라질 수 있다.
//      화면이 씬에 놓이는 순간 등록하면 어느 쪽 배치든 똑같이 동작한다.
//   2. 나중에 관전/원격 플레이어용 화면으로 갈아끼울 때 흐름 코드를 건드리지 않아도 된다.
public interface IAugmentSelectionView
{
    // 선택지를 띄운다. 고르거나 duration초가 지나면 onChosen이 정확히 한 번 호출된다.
    // 제한 시간이 끝났는데 고를 수 있는 선택지가 없었다면 null이 전달된다.
    //
    // 제한 시간을 화면이 아니라 GameManager가 정하는 이유: 이 시간은 화면 연출이 아니라
    // 매치 진행 속도라서, 다른 단계 시간과 한곳에서 같이 조절되어야 한다.
    void Show(AugmentManager manager, float duration, Action<AugmentData> onChosen);

    // 매치가 중단되어 화면을 걷어내야 할 때 부른다. onChosen은 호출되지 않는다.
    void Hide();
}
