using UnityEngine;

// Player의 자식인 Weapon 오브젝트에 부착. 무기/발사체 기본값으로부터
// 런타임 인스턴스를 만들고, 발사 위치(muzzle) 참조를 들고 있는다.
public class WeaponController : MonoBehaviour
{
    public WeaponStatsData baseData;
    public Transform muzzle;

    public WeaponStats Stats { get; private set; }
    public ProjectileStats ProjectileStats { get; private set; }

    void Awake()
    {
        Stats = new WeaponStats(baseData);
        ProjectileStats = new ProjectileStats(baseData.projectileStats);
    }
}
