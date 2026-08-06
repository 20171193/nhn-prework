// 게임 종료까지 안 바뀌는 플레이어 신상 정보. 네트워크로 전달되어야 해서
// Sprite 같은 Unity Object 참조 대신 원시 타입(문자열/인덱스)만 담는다.
// profileIconId는 로컬에서 프로필 아이콘 목록(PlayerHUD 등)을 찾을 때 쓰는 인덱스.
// weaponId는 선택한 무기를 가리키는 인덱스. 현재는 무기가 1종뿐이라 항상 0이지만
// 여러 무기 카탈로그가 생겨도 그대로 쓸 수 있도록 미리 둔다.
[System.Serializable]
public struct PlayerInfo
{
    public string playerName;
    public int profileIconId;
    public int weaponId;

    public PlayerInfo(string playerName, int profileIconId, int weaponId)
    {
        this.playerName = playerName;
        this.profileIconId = profileIconId;
        this.weaponId = weaponId;
    }
}
