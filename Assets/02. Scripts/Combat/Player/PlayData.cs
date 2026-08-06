using UnityEngine;

// 매치메이킹 전(싱글 환경)에서 정한 로컬 플레이어의 세팅을 들고 있는 저장소.
// Photon 의존이 없는 순수 정적 클래스라 오프라인 상태에서도 값을 세팅할 수 있고,
// 정적 필드라 씬을 넘어가도(로비 → 전투) 값이 그대로 유지된다.
// 기본값이 실제 DB에 존재하는 ID라서, 로비를 거치지 않고 전투 씬을 단독 실행해도
// 기본 무기/스킬로 바로 테스트할 수 있다.
// 실제 룸 진입 시 이 값을 상대방에게 전달하는 쪽은 PlayerSetupSync가 담당한다.
public static class PlayData
{
    const int DefaultWeaponId = 20001; // WeaponData_Gun
    const int DefaultThrowableWeaponId = 30002;  // Grenade(수류탄)
    const float DefaultMaxHp = 100f;
    const float DefaultMoveSpeed = 5f;

    static PlayerInfo current = new PlayerInfo(
        $"Player{Random.Range(1000, 10000)}", 0,
        DefaultWeaponId, DefaultThrowableWeaponId, DefaultMaxHp, DefaultMoveSpeed);

    public static PlayerInfo Current => current;

    public static void SetName(string playerName)
    {
        current.playerName = playerName;
    }

    public static void SetProfileIcon(int profileIconId)
    {
        current.profileIconId = profileIconId;
    }

    public static void SetWeapon(int weaponId)
    {
        current.weaponId = weaponId;
    }

    public static void SetThrowableWeapon(int throwableWeaponId)
    {
        current.throwableWeaponId = throwableWeaponId;
    }
}
