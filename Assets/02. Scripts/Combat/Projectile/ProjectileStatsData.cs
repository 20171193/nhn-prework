using UnityEngine;

// 발사체 자체의 기본 수치(데미지, 이동 속도 등).
[CreateAssetMenu(fileName = "ProjectileStatsData", menuName = "Combat/Projectile Stats")]
public class ProjectileStatsData : ScriptableObject
{
    [Header("발사체 데미지")]
    public float damage = 10f;
    [Header("발사체 이동 속도")]
    public float speed = 12f;
}
