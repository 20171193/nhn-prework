// ProjectileStatsData를 복사한 런타임 인스턴스.
[System.Serializable]
public class ProjectileStats
{
    public float damage;
    public float speed;

    public ProjectileStats(ProjectileStatsData data)
    {
        damage = data.damage;
        speed = data.speed;
    }
}
