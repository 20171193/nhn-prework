using UnityEngine;

// Player 루트에 부착. baseData(ScriptableObject 기본값)로부터 런타임 인스턴스를 만든다.
public class PlayerStatsController : MonoBehaviour
{
    public PlayerStatsData baseData;

    public PlayerStats Stats { get; private set; }

    void Awake()
    {
        Stats = new PlayerStats(baseData);
    }
}
