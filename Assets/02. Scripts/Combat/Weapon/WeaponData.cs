using UnityEngine;

public enum ProjectileTrajectory
{
    Straight,
}

// 무기 하나(수치+프리팹+아이콘)를 나타내는 데이터. id는 WeaponDatabase에서 조회할 때 쓰는
// 키로, 20000번대를 쓴다(카테고리별 구간: Projectile 10000~, Weapon 20000~, ThrowableWeapon 30000~).
// 데미지 등 발사체 고유 수치는 ProjectileData가 따로 갖는다.
// projectileData는 저작 시점에 정해지는 무기<->발사체 연결이라 ID 조회 없이 직접 참조한다
// (네트워크로 전달되는 값이 아니므로 PlayerInfo.weaponId 같은 원시 타입 제약이 적용 안 됨).
[CreateAssetMenu(fileName = "WeaponData", menuName = "Combat/Weapon Data")]
public class WeaponData : ScriptableObject, IHasId
{
    [Header("ID (20000~)")]
    public int id;
    public int Id => id;

    public ProjectileTrajectory trajectory = ProjectileTrajectory.Straight;
    [Header("-1 = 무한")]
    public float projectileRange = 10f;
    [Header("발사체 개수")]
    [Range(1, 5)] public int projectileCount = 1;
    [Header("초당 발사 횟수")]
    [Range(0.1f, 10f)] public float attackRate = 2f; // 초당 발사 횟수 (공격 후 다음 공격까지 딜레이의 역수)
    [Header("발사체 퍼짐 각도")]
    [Range(0.1f, 8f)] public float spreadAngleDegrees = 8f; // 발사체가 여러 개일 때 부채꼴로 벌어지는 간격

    public ProjectileData projectileData;

    [Header("표시/스폰")]
    public GameObject prefab;
    public Sprite icon;
}
