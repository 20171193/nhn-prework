// 게임 종료까지 안 바뀌는 플레이어 신상 정보. 네트워크로 전달되어야 해서
// Sprite 같은 Unity Object 참조 대신 원시 타입(문자열/인덱스)만 담는다.
// profileIconId는 로컬에서 프로필 아이콘 목록(PlayerHUD 등)을 찾을 때 쓰는 인덱스.
// weaponId/throwableWeaponId는 WeaponDatabase/ThrowableWeaponDatabase에서 조회할 때 쓰는 ID.
// maxHp/moveSpeed는 지금은 전원 고정값이지만, 나중에 캐릭터 선택이 생기면
// 캐릭터마다 다른 값을 로비에서 골라 담을 수 있도록 미리 둔다.
[System.Serializable]
public struct PlayerInfo
{
    public string playerName;
    public int profileIconId;
    public int weaponId;
    public int throwableWeaponId;
    public float maxHp;
    public float moveSpeed;

    public PlayerInfo(string playerName, int profileIconId, int weaponId, int throwableWeaponId, float maxHp, float moveSpeed)
    {
        this.playerName = playerName;
        this.profileIconId = profileIconId;
        this.weaponId = weaponId;
        this.throwableWeaponId = throwableWeaponId;
        this.maxHp = maxHp;
        this.moveSpeed = moveSpeed;
    }
}
