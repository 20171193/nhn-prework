// PlayerStatsData를 복사한 런타임 인스턴스. 증강 등으로 여기 값만 변경하고
// baseData(ScriptableObject) 원본은 건드리지 않는다.
[System.Serializable]
public class PlayerStats
{
    public float maxHp;
    public float currentHp;
    public float moveSpeed;

    public PlayerStats(PlayerStatsData data)
    {
        maxHp = data.maxHp;
        currentHp = data.maxHp;
        moveSpeed = data.moveSpeed;
    }
}
