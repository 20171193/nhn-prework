// PlayData(로비 세팅)에서 온 기준값의 런타임 인스턴스. 증강 등으로 여기 값만 변경한다.
[System.Serializable]
public class PlayerStats
{
    public float maxHp;
    public float currentHp;
    public float moveSpeed;

    public PlayerStats(float maxHp, float moveSpeed)
    {
        this.maxHp = maxHp;
        currentHp = maxHp;
        this.moveSpeed = moveSpeed;
    }
}
