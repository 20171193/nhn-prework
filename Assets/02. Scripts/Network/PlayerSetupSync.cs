using Photon.Pun;
using Photon.Realtime;
using Hashtable = ExitGames.Client.Photon.Hashtable;

// LocalPlayerSetup(싱글 환경에서 정한 값)을 Photon CustomProperties로 발행/조회한다.
// CustomProperties는 룸 Join 오퍼레이션에 함께 실려 전달되므로, 연결/입장 "전"에
// PublishLocal을 호출해두면 상대방은 내가 룸에 들어오는 시점에 이미 값을 갖고 있다.
public static class PlayerSetupSync
{
    const string NameKey = "pn";
    const string ProfileIconKey = "pi";
    const string WeaponKey = "pw";

    public static void PublishLocal(PlayerInfo info)
    {
        var props = new Hashtable
        {
            { NameKey, info.playerName },
            { ProfileIconKey, info.profileIconId },
            { WeaponKey, info.weaponId }
        };

        PhotonNetwork.LocalPlayer.SetCustomProperties(props);
    }

    public static bool TryRead(Player player, out PlayerInfo info)
    {
        info = default;

        if (player == null) return false;

        var props = player.CustomProperties;
        if (!props.TryGetValue(NameKey, out var nameValue) ||
            !props.TryGetValue(ProfileIconKey, out var iconValue) ||
            !props.TryGetValue(WeaponKey, out var weaponValue))
            return false;

        info = new PlayerInfo((string)nameValue, (int)iconValue, (int)weaponValue);
        return true;
    }
}
