// WeaponStatsData를 복사한 런타임 인스턴스.
[System.Serializable]
public class WeaponStats
{
    public ProjectileTrajectory trajectory;
    public float projectileRange;
    public int projectileCount;
    public float attackRate;

    public WeaponStats(WeaponStatsData data)
    {
        trajectory = data.trajectory;
        projectileRange = data.projectileRange;
        projectileCount = data.projectileCount;
        attackRate = data.attackRate;
    }
}
