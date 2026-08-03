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
    public float projectileRange = 10f;
    public int projectileCount = 1;
    public float attackRate = 2f; // 초당 발사 횟수 (공격 후 다음 공격까지 딜레이의 역수)

    public ProjectileStatsData projectileStats;
}
