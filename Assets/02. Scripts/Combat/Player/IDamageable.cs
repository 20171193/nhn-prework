// 데미지를 받을 수 있는 대상의 계약. Projectile 등 가해자 쪽은 구체 타입(PlayerStatsController)을
// 몰라도 되고, 나중에 플레이어 외의 피격 대상(파괴 가능한 오브젝트 등)이 생겨도 그대로 재사용한다.
public interface IDamageable
{
    void ApplyDamage(float amount);
}
