// SfxData.id와 1:1로 대응한다 - 값 자체가 곧 SfxDatabase 조회 ID(60000~)다.
// 새 효과음을 추가하면 여기에도 항목을 추가하고, 데이터 에셋의 id를 같은 값으로 맞춘다.
public enum SfxId
{
    WeaponFire = 60001,
    GrenadeExplode = 60002,
    ClickAugmentSelectBTN = 60003,
    AugmentSelectTimer = 60004, // 루프 
    BulletHitObstacle = 60005,
    PlayerDamaged = 60006,
    PlayerMove = 60007,
    ClickNormalBTN = 60008,
    ThrowWeapon = 60009,
    ThrowableWeaponLanding = 60010,
    FlashbangPop = 60011,
    FlashbangEffect = 60012,
    SmokePop = 60013,
}
