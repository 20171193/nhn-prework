using System;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

// 증강 시스템이 실제로 붙는 지점. PlayerStats/WeaponStats/ProjectileStats의
// base 값은 절대 직접 건드리지 않고, 증강은 AddModifier()로 목록에만 쌓는다.
// 스탯이 바뀔 수 있는 시점은 "증강 선택" 하나뿐이므로(전투 중에는 base도
// modifier도 변하지 않음) 매 프레임 재계산하지 않고, 그 시점에만 미리 계산해
// Effective* 값으로 캐싱해둔다. 이동/조준/발사 등 실제 로직은 이 값을 그대로 읽어서 쓴다.
//
// 발사 자체는 RPC로 전파한다 - PhotonView가 이 오브젝트(Player 루트)에 있어서
// RPC 수신([PunRPC])도 여기 있어야 한다. Projectile은 더 이상 네트워크 오브젝트가
// 아니라 각 클라이언트가 RPC를 받아 로컬로 직접 생성한다.
public class PlayerCombatContext : MonoBehaviourPun
{
    public PlayerStatsController playerStatsController;
    public WeaponController weaponController;
    public Collider2D hitCollider; // 이 플레이어를 맞힐 수 있는 콜라이더. 자신이 쏜 발사체가 이걸 무시하도록 넘겨줄 때 씀

    readonly List<StatModifier> modifiers = new List<StatModifier>();

    public float EffectiveMoveSpeed { get; private set; }
    public float EffectiveAttackRate { get; private set; }
    public float EffectiveProjectileSpeed { get; private set; }
    public float EffectiveProjectileDamage { get; private set; }
    public int EffectiveProjectileCount { get; private set; }

    // 증강 선택 등 입력을 받으면 안 되는 구간에 GameManager(게임 플로우)가 끈다.
    // Movement/Aim/Weapon 각 입력 컨트롤러가 Update에서 이 값을 확인한다.
    public bool InputEnabled { get; private set; } = true;

    public void SetInputEnabled(bool enabled)
    {
        InputEnabled = enabled;
    }

    // 실제로 획득에 성공한 증강 하나를 UI(HUD 등)에 알린다. PlayerCombatContext는
    // 누가 듣는지 모르고, HUD 쪽이 이 이벤트를 구독해서 아이콘을 채운다.
    public event Action<AugmentData> OnAugmentAcquired;

    void OnEnable()
    {
        playerStatsController.OnDeath += HandleDeath;
    }
    void OnDisable()
    {
        playerStatsController.OnDeath -= HandleDeath;
    }

    void HandleDeath()
    {
        SetInputEnabled(false);
        
        // todo
        // GameManager.Instance.OnPlayerDeath(photonView.Owner);
    }

    void Start()
    {
        Recalculate();
    }

    public void FireVolley(Vector3 muzzlePosition, float[] payload)
    {
        photonView.RPC(nameof(RpcFireVolley), RpcTarget.All, muzzlePosition, payload);
    }

    [PunRPC]
    void RpcFireVolley(Vector3 muzzlePosition, float[] payload)
    {
        ProjectileVolleyData.Unpack(payload, out float speed, out float damage, out float range, out Vector2[] directions);

        foreach (var dir in directions)
        {
            var proj = ProjectilePool.Get(muzzlePosition, Quaternion.identity);
            proj.GetComponent<Projectile>().Init(dir, speed, damage, range, hitCollider);
        }
    }

    // 증강 선택 UI(증강 개발자 쪽)가 플레이어가 고른 증강을 최종 확정할 때 호출하는 지점.
    public void ApplyAugment(AugmentData augment)
    {
        augment.Apply(this);
        OnAugmentAcquired?.Invoke(augment);
    }
    public void AddModifier(StatModifier modifier)
    {
        modifiers.Add(modifier);
        Recalculate();
    }
    void Recalculate()
    {
        var playerStats = playerStatsController.Stats;
        var weaponStats = weaponController.Stats;
        var projectileStats = weaponController.ProjectileStats;

        EffectiveMoveSpeed = Calculate(playerStats.moveSpeed, StatType.MoveSpeed);
        EffectiveAttackRate = Calculate(weaponStats.attackRate, StatType.AttackRate);
        EffectiveProjectileSpeed = Calculate(projectileStats.speed, StatType.ProjectileSpeed);
        EffectiveProjectileDamage = Calculate(projectileStats.damage, StatType.ProjectileDamage);
        EffectiveProjectileCount = Mathf.Max(1, Mathf.RoundToInt(Calculate(weaponStats.projectileCount, StatType.ProjectileCount)));
    }
    float Calculate(float baseValue, StatType statType)
    {
        float sumOfAdds = 0f;
        float productOfMultiplies = 1f;

        foreach (var modifier in modifiers)
        {
            if (modifier.statType != statType) continue;

            if (modifier.operation == StatOperation.Add)
                sumOfAdds += modifier.value;
            else
                productOfMultiplies *= modifier.value;
        }

        return (baseValue + sumOfAdds) * productOfMultiplies;
    }
}
