using TMPro;
using UnityEngine;

// 화면 우측 스텟 메뉴. -/+ 버튼으로 PlayerController의 수치를 런타임에 조절한다.
public class StatMenuUI : MonoBehaviour
{
    public PlayerController player;

    public TMP_Text attackRateLabel;
    public TMP_Text projectileSpeedLabel;
    public TMP_Text moveSpeedLabel;

    public float attackRateStep = 0.1f;
    public float projectileSpeedStep = 0.1f;
    public float moveSpeedStep = 0.1f;

    const float MinValue = 0.1f;

    void Start()
    {
        RefreshAll();
    }

    public void IncreaseAttackRate() => ChangeAttackRate(attackRateStep);
    public void DecreaseAttackRate() => ChangeAttackRate(-attackRateStep);

    public void IncreaseProjectileSpeed() => ChangeProjectileSpeed(projectileSpeedStep);
    public void DecreaseProjectileSpeed() => ChangeProjectileSpeed(-projectileSpeedStep);

    public void IncreaseMoveSpeed() => ChangeMoveSpeed(moveSpeedStep);
    public void DecreaseMoveSpeed() => ChangeMoveSpeed(-moveSpeedStep);

    void ChangeAttackRate(float delta)
    {
        player.attackRate = Mathf.Max(MinValue, player.attackRate + delta);
        RefreshAttackRate();
    }

    void ChangeProjectileSpeed(float delta)
    {
        player.projectileSpeed = Mathf.Max(MinValue, player.projectileSpeed + delta);
        RefreshProjectileSpeed();
    }

    void ChangeMoveSpeed(float delta)
    {
        player.moveSpeed = Mathf.Max(MinValue, player.moveSpeed + delta);
        RefreshMoveSpeed();
    }

    void RefreshAll()
    {
        RefreshAttackRate();
        RefreshProjectileSpeed();
        RefreshMoveSpeed();
    }

    void RefreshAttackRate() => attackRateLabel.text = $"Attack Speed (p/s) {player.attackRate:0.0}";
    void RefreshProjectileSpeed() => projectileSpeedLabel.text = $"Projectile Speed {player.projectileSpeed:0.0}";
    void RefreshMoveSpeed() => moveSpeedLabel.text = $"Move Speed {player.moveSpeed:0.0}";
}
