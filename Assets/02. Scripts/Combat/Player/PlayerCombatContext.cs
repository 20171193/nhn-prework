using System;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;
using UnityEngine.Serialization;

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
    PlayerWeaponFireController fireController; // weaponController와 같은 무기 인스턴스에 붙어있다 - EquipWeapon에서 함께 캐싱.
    // 증강으로 투척무기 슬롯을 잠금해제할 때 씀 (context.throwableWeaponController.Equip(i, data)).
    [FormerlySerializedAs("throwableSkillController")]
    public ThrowableWeaponController throwableWeaponController;
    public Collider2D hitCollider; // 이 플레이어를 맞힐 수 있는 콜라이더. 자신이 쏜 발사체가 이걸 무시하도록 넘겨줄 때 씀

    [SerializeField] private Transform weaponSocket; // EquipWeapon이 무기 프리팹을 여기 자식으로 스폰한다.

    // 투척무기 에임 중 소켓에 붙는 미리보기 전용 인스턴스(실제로 날아가는 인스턴스와는 별개).
    GameObject throwablePreviewInstance;

    readonly List<StatModifier> modifiers = new List<StatModifier>();

    public float EffectiveMoveSpeed { get; private set; }
    public float EffectiveAttackRate { get; private set; }
    public float EffectiveProjectileSpeed { get; private set; }
    public float EffectiveProjectileDamage { get; private set; }
    public int EffectiveProjectileCount { get; private set; }

    // 증강 선택에서 고른 증강을 받을 대상으로 자신을 등록한다.
    // GameManager -> 플레이어 방향의 참조를 두지 않는 이유는 IAugmentSelectionView 주석과 같다.
    // (플레이어는 씬과 함께 사라지지만 GameManager는 씬을 넘어 살아남는다.)
    //
    // 한 클라이언트에는 Player가 둘 있다(내 것과 상대의 원격 사본). 소유 검사를 빼면
    // 나중에 생긴 원격 사본이 등록을 덮어써서, 내가 고른 증강이 상대 사본에 붙는다.
    // 그 사본의 이동/발사는 IsMine이 아니라 아무 일도 하지 않으므로 증강이 통째로 사라진다.
    void OnEnable()
    {
        playerStatsController.OnDeath += HandleDeath;

        if (!photonView.IsMine) return;

        if (GameManager.Instance != null)
            GameManager.Instance.RegisterLocalPlayer(this);
    }

    void OnDisable()
    {
        playerStatsController.OnDeath -= HandleDeath;

        if (GameManager.Instance != null)
            GameManager.Instance.UnregisterLocalPlayer(this);
    }

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

    // DB에서 찾은 무기 프리팹을 weaponSocket 아래 스폰하고 weaponController를 채운다.
    // Weapon 프리팹이 언제 생기든(정적이 아니라 여기서 동적으로 생기므로) 자식 쪽 컴포넌트들은
    // GetComponentInParent로 이 오브젝트를 스스로 찾아간다 - 반대 방향(부모->자식)만 이렇게 주입해준다.
    public void EquipWeapon(GameObject weaponPrefab)
    {
        var instance = Instantiate(weaponPrefab, weaponSocket);
        weaponController = instance.GetComponent<WeaponController>();
        fireController = instance.GetComponent<PlayerWeaponFireController>();
        Recalculate();
    }

    public void FireVolley(Vector3 muzzlePosition, float[] payload)
    {
        photonView.RPC(nameof(RpcFireVolley), RpcTarget.All, muzzlePosition, payload);
    }

    [PunRPC]
    void RpcFireVolley(Vector3 muzzlePosition, float[] payload)
    {
        ProjectileVolleyData.Unpack(payload, out int projectileId, out float speed, out float damage, out float range, out Vector2[] directions);

        if (!ProjectileDatabase.Instance.TryGet(projectileId, out var projectileData))
        {
            Debug.LogWarning($"projectileId {projectileId}에 해당하는 발사체를 찾지 못했습니다.", this);
            return;
        }

        foreach (var dir in directions)
        {
            var proj = ProjectilePool.Get(projectileData.prefab, muzzlePosition, Quaternion.identity);
            proj.GetComponent<Projectile>().Init(dir, speed, damage, range, hitCollider);
        }

        fireController?.weaponAnimator.SetTrigger("OnFire");
        SoundManager.Instance?.PlaySfx(SfxId.WeaponFire, muzzlePosition);
    }

    // FireVolley와 같은 패턴 - 투척도 RPC로 전파하고, 각 클라이언트가 로컬로 독립 재생한다.
    public void Throw(Vector3 start, Vector3 end, int throwableWeaponId)
    {
        photonView.RPC(nameof(RpcThrow), RpcTarget.All, start, end, throwableWeaponId);
    }

    [PunRPC]
    void RpcThrow(Vector3 start, Vector3 end, int throwableWeaponId)
    {
        if (!ThrowableWeaponDatabase.Instance.TryGet(throwableWeaponId, out var data) || data.prefab == null)
        {
            Debug.LogWarning($"throwableWeaponId {throwableWeaponId}에 해당하는 투척물을 찾지 못했습니다.", this);
            return;
        }

        var obj = ProjectilePool.Get(data.prefab, start, Quaternion.identity);
        obj.GetComponent<ThrowableObject>().Init(start, end, data);

        // 수류탄/섬광탄/연막탄 3종 모두 동일한 투척음을 쓴다.
        SoundManager.Instance?.PlaySfx(SfxId.ThrowWeapon, start);
    }

    // Throw와 같은 패턴 - 에임 시작/슬롯 전환도 RPC로 전파해서 상대방 화면에도 총이
    // 숨겨지고 투척무기를 든 모습이 보이게 한다. 로컬 자기 자신도 이 RPC를 통해서만
    // 반영한다(직접 호출 X) - 그래야 로컬/원격이 항상 같은 경로로 같은 상태를 갖는다.
    public void SetThrowablePreview(int throwableWeaponId)
    {
        photonView.RPC(nameof(RpcSetThrowablePreview), RpcTarget.All, throwableWeaponId);
    }

    [PunRPC]
    void RpcSetThrowablePreview(int throwableWeaponId)
    {
        if (!ThrowableWeaponDatabase.Instance.TryGet(throwableWeaponId, out var data) || data.prefab == null)
        {
            Debug.LogWarning($"throwableWeaponId {throwableWeaponId}에 해당하는 투척무기를 찾지 못했습니다.", this);
            return;
        }

        ShowThrowablePreview(data.prefab);
    }

    public void ClearThrowablePreview()
    {
        photonView.RPC(nameof(RpcClearThrowablePreview), RpcTarget.All);
    }

    [PunRPC]
    void RpcClearThrowablePreview()
    {
        HideThrowablePreview();
    }

    // 숫자키로 투척무기 에임에 들어갔을 때, 실제로 던지는 것과 같은 프리팹을 ProjectilePool에서
    // 꺼내 소켓에 붙여 미리보기로 보여준다(Instantiate 아님 - 실제 투척과 같은 풀 재사용).
    // 총 자체는 그대로 두고 스프라이트만 숨긴다.
    void ShowThrowablePreview(GameObject prefab)
    {
        // 슬롯 간 스왑처럼 미리보기가 이미 떠 있는 상태로 다시 불릴 수 있다 - 새로 꺼내기 전에
        // 이전 미리보기부터 반납해야 소켓에 두 개가 겹치거나 이전 것이 안 꺼지는 문제가 없다.
        ReleaseThrowablePreviewInstance();

        if (weaponController != null && weaponController.weaponSprite != null)
            weaponController.weaponSprite.enabled = false;

        if (prefab == null) return;

        throwablePreviewInstance = ProjectilePool.Get(prefab, weaponSocket.position, weaponSocket.rotation);
        throwablePreviewInstance.transform.SetParent(weaponSocket); // 플레이어를 따라 움직이도록

        var throwable = throwablePreviewInstance.GetComponent<ThrowableObject>();
        if (throwable != null) throwable.enabled = false; // 미리보기는 날아가는 로직이 돌면 안 됨
    }

    void HideThrowablePreview()
    {
        ReleaseThrowablePreviewInstance();

        if (weaponController != null && weaponController.weaponSprite != null)
            weaponController.weaponSprite.enabled = true;
    }

    void ReleaseThrowablePreviewInstance()
    {
        if (throwablePreviewInstance == null) return;

        var throwable = throwablePreviewInstance.GetComponent<ThrowableObject>();
        if (throwable != null) throwable.enabled = true; // 다음에 이 인스턴스가 실제로 던져질 때를 대비해 원복

        ProjectilePool.Release(throwablePreviewInstance);
        throwablePreviewInstance = null;
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
        // playerStatsController.Stats/weaponController는 각각 Init/EquipWeapon으로 동적으로
        // 채워져서, Start()가 먼저 돌면 아직 비어있을 수 있다 - 그때는 조용히 넘어가고
        // Init/EquipWeapon 쪽이 다시 불러준다.
        if (playerStatsController == null || playerStatsController.Stats == null || weaponController == null) return;

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
