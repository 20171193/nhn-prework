using UnityEngine;

// 디자이너가 인스펙터에서 튜닝하는 플레이어 기본 수치.
[CreateAssetMenu(fileName = "PlayerStatsData", menuName = "Combat/Player Stats")]
public class PlayerStatsData : ScriptableObject
{
    public float maxHp = 100f;
    public float moveSpeed = 5f;
}
