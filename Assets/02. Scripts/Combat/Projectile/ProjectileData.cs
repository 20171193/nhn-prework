using UnityEngine;

// 발사체 하나(수치+프리팹+아이콘)를 나타내는 데이터. id는 ProjectileDatabase에서 조회할 때 쓰는
// 키로, 10000번대를 쓴다(카테고리별 구간: Projectile 10000~, Weapon 20000~, ThrowableWeapon 30000~).
[CreateAssetMenu(fileName = "ProjectileData", menuName = "Combat/Projectile Data")]
public class ProjectileData : ScriptableObject, IHasId
{
    [Header("ID (10000~)")]
    public int id;
    public int Id => id;

    [Header("발사체 데미지")]
    public float damage = 10f;
    [Header("발사체 이동 속도")]
    public float speed = 12f;

    [Header("표시/스폰")]
    public GameObject prefab;
    public Sprite icon;
}
