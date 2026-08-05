using UnityEngine;

public enum ProjectileTrajectory
{
    Straight,
}

// 무기 자체의 기본 수치(발사체 궤적/이동 거리/개수/공격 속도).
// 데미지 등 발사체 고유 수치는 ProjectileStatsData가 따로 갖는다.
[CreateAssetMenu(fileName = "WeaponStatsData", menuName = "Combat/Weapon Stats")]
public class WeaponStatsData : ScriptableObject
{
    public ProjectileTrajectory trajectory = ProjectileTrajectory.Straight;
    [Header("-1 = 무한")]
    public float projectileRange = 10f;
    [Header("발사체 개수")]
    [Range(1, 5)] public int projectileCount = 1;
    [Header("초당 발사 횟수")]
    [Range(0.1f, 10f)] public float attackRate = 2f; // 초당 발사 횟수 (공격 후 다음 공격까지 딜레이의 역수)
    [Header("발사체 퍼짐 각도")]
    [Range(0.1f, 8f)] public float spreadAngleDegrees = 8f; // 발사체가 여러 개일 때 부채꼴로 벌어지는 간격

    public ProjectileStatsData projectileStats;
}
