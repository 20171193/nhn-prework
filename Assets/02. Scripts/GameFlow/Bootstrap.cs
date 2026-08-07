using UnityEngine;

// 앱이 시작될 때 씬과 무관하게 존재해야 하는 객체를 만든다.
//
// 매치메이킹 씬에 GameManager를 놓아두는 것만으로는 부족하다. 전투 씬을 단독으로 열어
// 테스트하는 경로(CombatNetworkManager)가 있고, 그 경우 GameManager가 아예 없기 때문이다.
// 여기서 만들면 어느 씬에서 플레이를 시작하든 똑같이 동작하고, 씬 파일을 건드릴 일도 없다.
//
// BeforeSceneLoad라서 어떤 씬 오브젝트의 Awake/OnEnable보다 먼저 돈다.
// 덕분에 AugmentSelectionUI가 OnEnable에서 GameManager.Instance를 안전하게 쓸 수 있다.
public static class Bootstrap
{
    // Assets/Resources 기준 경로. 확장자 없이 이름만 쓴다.
    const string GameManagerPath = "GameManager";
    const string CursorManagerPath = "CursorManager";
    const string LobbyPlayerSetupPath = "LobbyPlayerSetup";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    public static void Initialize()
    {
        // 중복 생성 방지. 씬에 이미 인스턴스가 놓여 있어도 Singleton이 정리해주지만,
        // 애초에 만들지 않는 편이 로그가 깨끗하다.
        if (GameManager.Instance != null) return;

        var prefab = Resources.Load<GameManager>(GameManagerPath);
        if (prefab == null)
        {
            Debug.LogError($"Resources/{GameManagerPath} 프리팹을 찾을 수 없습니다. 프리팹이 Assets/Resources 아래에 있는지 확인하세요.");
            return;
        }

        // static 클래스라 MonoBehaviour의 Instantiate를 쓸 수 없다.
        UnityEngine.Object.Instantiate(prefab);
    }

    // 게임 시작부터 OS 커서를 숨기고 커스텀 커서를 띄워야 하므로 GameManager와 동일하게
    // 부팅 시점에 미리 만들어둔다(로비/게임룸 어느 씬으로 시작하든 항상 존재).
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    public static void InitializeCursorManager()
    {
        if (CursorManager.Instance != null) return;

        var prefab = Resources.Load<CursorManager>(CursorManagerPath);
        if (prefab == null)
        {
            Debug.LogError($"Resources/{CursorManagerPath} 프리팹을 찾을 수 없습니다. 프리팹이 Assets/Resources 아래에 있는지 확인하세요.");
            return;
        }

        UnityEngine.Object.Instantiate(prefab);
    }

    // 전투 데이터 DB(Weapon/Projectile/ThrowableWeapon)도 게임 시작 시 한 번만 Resources에서 불러
    // static Instance에 캐싱해둔다. 인게임 중에 처음 발사/장착하는 순간 로딩 비용이
    // 튀지 않도록, 실제로 쓰이기 훨씬 전인 부팅 시점에 미리 끝내두는 것.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    public static void InitializeCombatDatabases()
    {
        WeaponDatabase.EnsureLoaded();
        ProjectileDatabase.EnsureLoaded();
        ThrowableWeaponDatabase.EnsureLoaded();
    }

    // LobbyPlayerSetup.Awake가 WeaponDatabase/ThrowableWeaponDatabase.Instance.entries를 바로
    // 읽으므로, InitializeCombatDatabases가 먼저 끝나 있어야 한다 - 같은 클래스 내
    // RuntimeInitializeOnLoadMethod는 선언 순서대로 실행되므로 일부러 그 아래에 선언한다.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    public static void InitializeLobbyPlayerSetup()
    {
        if (LobbyPlayerSetup.Instance != null) return;

        var prefab = Resources.Load<LobbyPlayerSetup>(LobbyPlayerSetupPath);
        if (prefab == null)
        {
            Debug.LogError($"Resources/{LobbyPlayerSetupPath} 프리팹을 찾을 수 없습니다. 프리팹이 Assets/Resources 아래에 있는지 확인하세요.");
            return;
        }

        UnityEngine.Object.Instantiate(prefab);
    }
}
