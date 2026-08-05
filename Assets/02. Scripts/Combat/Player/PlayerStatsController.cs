using System;
using Photon.Pun;
using UnityEngine;

// Player 루트에 부착. baseData(ScriptableObject 기본값)로부터 런타임 인스턴스를 만든다.
// HP 등 런타임에 바뀌는 값은 여기서 이벤트로 알리기만 하고, 누가 듣는지(HUD 등)는 모른다.
// UI는 이 이벤트를 구독해서 반응하고, 반대로 여기서 UI를 직접 참조하지 않는다.
//
// IDamageable.ApplyDamage는 로컬에서 바로 HP를 깎지 않는다 - 판정 권한은 발사체
// 소유자(쏜 사람)에게만 있고, 그 판정 결과를 RPC로 모든 클라이언트에 동일하게 전파해서
// 반영한다(각 클라이언트가 독립적으로 판정하면 히트가 중복 반영되거나 HP가 어긋날 수 있음).
public class PlayerStatsController : MonoBehaviourPun, IDamageable
{
    public PlayerStatsData baseData;

    public PlayerStats Stats { get; private set; }

    // 파라미터: (currentHp, maxHp)
    public event Action<float, float> OnHpChanged;

    void Awake()
    {
        Stats = new PlayerStats(baseData);
    }

    public void ApplyDamage(float amount)
    {
        photonView.RPC(nameof(RpcApplyDamage), RpcTarget.All, amount);
    }

    [PunRPC]
    void RpcApplyDamage(float amount)
    {
        if (amount <= 0f) return;

        Stats.currentHp = Mathf.Max(0f, Stats.currentHp - amount);
        OnHpChanged?.Invoke(Stats.currentHp, Stats.maxHp);
    }
}
