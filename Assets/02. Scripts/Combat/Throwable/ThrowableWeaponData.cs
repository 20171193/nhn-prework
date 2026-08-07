using UnityEngine;

// 투척무기 하나(수치+프리팹+아이콘)를 나타내는 데이터. id는 ThrowableWeaponDatabase에서 조회할 때 쓰는
// 키로, 30000번대를 쓴다(카테고리별 구간: Projectile 10000~, Weapon 20000~, ThrowableWeapon 30000~).
[CreateAssetMenu(fileName = "ThrowableWeaponData", menuName = "Combat/Throwable Weapon Data")]
public class ThrowableWeaponData : ScriptableObject, IHasId
{
    [Header("ID (30000~)")]
    public int id;
    public int Id => id;

    public float cooldown = 10f;
    public float range = 6f; // 투척 가능 거리(에임 사거리) - 트리거 판정 반경과는 별개

    [Header("트리거")]
    public float triggerDelay = 1f; // 도착(또는 벽에 막힌 지점)부터 트리거까지 걸리는 시간
    public float effectRadius = 2f; // 트리거 시 효과 판정 반경
    // 타입마다 의미가 다른 범용 수치 - 수류탄은 데미지, 섬광탄/연막탄은 지속시간(초).
    public float effectValue;

    [Header("표시/스폰")]
    public GameObject prefab;
    public Sprite icon;
}
