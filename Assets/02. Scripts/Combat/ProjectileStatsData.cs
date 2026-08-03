using UnityEngine;

// 발사체 자체의 기본 수치(데미지, 이동 속도 등).
[CreateAssetMenu(fileName = "ProjectileStatsData", menuName = "Combat/Projectile Stats")]
public class ProjectileStatsData : ScriptableObject
{
    public float damage = 10f;
    public float speed = 12f;
}
