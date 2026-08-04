// 증강 시스템(개발2)이 구현하는 계약.
// 증강 하나를 획득하면 Apply()에서 context.AddModifier(...)를 호출해
// 자신의 효과를 등록한다. 실제 수치 반영은 PlayerCombatContext가 매 프레임 계산한다.
public interface IAugmentEffect
{
    void Apply(PlayerCombatContext context);
}
