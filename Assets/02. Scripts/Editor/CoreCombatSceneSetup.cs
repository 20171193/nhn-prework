using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Tools > Combat Prototype > Add Core Combat Stats 실행 시
// 기본값 ScriptableObject를 만들고, 씬의 Player/Weapon(Grip)/Shoulder에
// 코어 전투 컴포넌트(스탯/이동/조준/발사/증강 컨텍스트)를 부착하고
// Resources의 Projectile 프리팹에 이동/충돌 컴포넌트를 채워 넣는다.
// 각 단계는 이미 되어 있으면 건너뛰므로 여러 번 실행해도 안전하다.
// 네트워크와 무관하게 싱글플레이로 테스트한다.
public static class CoreCombatSceneSetup
{
    const string DataFolder = "Assets/02. Scripts/Combat/Data";
    const string ProjectilePrefabPath = "Assets/Resources/Projectile.prefab";

    [MenuItem("Tools/Combat Prototype/Add Core Combat Stats")]
    static void Build()
    {
        var player = GameObject.Find("Player");
        if (player == null)
        {
            Debug.LogWarning("씬에서 Player를 찾을 수 없습니다.");
            return;
        }

        EnsureDataFolder();

        var playerStatsData = GetOrCreateAsset<PlayerStatsData>($"{DataFolder}/PlayerStatsData_Default.asset");
        var projectileStatsData = GetOrCreateAsset<ProjectileStatsData>($"{DataFolder}/ProjectileStatsData_Default.asset");
        var weaponStatsData = GetOrCreateAsset<WeaponStatsData>($"{DataFolder}/WeaponStatsData_Default.asset");
        if (weaponStatsData.projectileStats == null)
        {
            weaponStatsData.projectileStats = projectileStatsData;
            EditorUtility.SetDirty(weaponStatsData);
        }

        var statsController = player.GetComponent<PlayerStatsController>();
        if (statsController == null)
        {
            statsController = player.AddComponent<PlayerStatsController>();
            statsController.baseData = playerStatsData;
        }

        var grip = player.transform.Find("Grip");
        var weaponTransform = player.transform.Find("Weapon");
        GameObject weaponObj;
        WeaponController weaponController;
        if (weaponTransform == null)
        {
            weaponObj = new GameObject("Weapon");
            weaponObj.transform.SetParent(player.transform, false);

            weaponController = weaponObj.AddComponent<WeaponController>();
            weaponController.baseData = weaponStatsData;
            weaponController.muzzle = grip != null ? grip : weaponObj.transform;
            if (grip == null)
                Debug.LogWarning("Player 아래에서 Grip을 못 찾아 Weapon 자신을 muzzle로 사용했습니다.");
        }
        else
        {
            weaponObj = weaponTransform.gameObject;
            weaponController = weaponObj.GetComponent<WeaponController>();
        }

        // 증강 시스템이 실제로 붙는 지점. Player/Weapon 컴포넌트가 준비된 뒤에 연결한다.
        var combatContext = player.GetComponent<PlayerCombatContext>();
        if (combatContext == null)
            combatContext = player.AddComponent<PlayerCombatContext>();
        combatContext.playerStatsController = statsController;
        combatContext.weaponController = weaponController;

        if (player.GetComponent<PlayerMovementController>() == null)
            player.AddComponent<PlayerMovementController>();

        var aimController = player.GetComponent<PlayerAimController>();
        if (aimController == null)
        {
            aimController = player.AddComponent<PlayerAimController>();
            var shoulder = player.transform.Find("Shoulder");
            aimController.shoulder = shoulder;
            if (shoulder == null)
                Debug.LogWarning("Player 아래에서 Shoulder를 못 찾았습니다. PlayerAimController.shoulder를 직접 연결해주세요.");
        }

        if (weaponObj.GetComponent<PlayerWeaponFireController>() == null)
        {
            var fireController = weaponObj.AddComponent<PlayerWeaponFireController>();
            fireController.aim = aimController;
        }

        EnsureProjectilePrefabComponents();

        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Selection.activeGameObject = player;
        Debug.Log("코어 전투 스탯 세팅 완료. Ctrl+S로 씬을 저장하세요. (CombatNetworkManager는 싱글 테스트 동안 꺼두세요)");
    }

    static void EnsureProjectilePrefabComponents()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ProjectilePrefabPath);
        if (prefab == null)
        {
            Debug.LogWarning($"{ProjectilePrefabPath}를 찾을 수 없습니다.");
            return;
        }

        if (prefab.GetComponent<Projectile>() != null) return; // 이미 세팅됨

        using (var editScope = new PrefabUtility.EditPrefabContentsScope(ProjectilePrefabPath))
        {
            var root = editScope.prefabContentsRoot;

            var rb = root.GetComponent<Rigidbody2D>();
            if (rb == null) rb = root.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.gravityScale = 0f;

            var col = root.GetComponent<CircleCollider2D>();
            if (col == null) col = root.AddComponent<CircleCollider2D>();
            col.isTrigger = true;

            if (root.GetComponent<Projectile>() == null)
                root.AddComponent<Projectile>();
        }
    }

    static void EnsureDataFolder()
    {
        if (!AssetDatabase.IsValidFolder(DataFolder))
            AssetDatabase.CreateFolder("Assets/02. Scripts/Combat", "Data");
    }

    static T GetOrCreateAsset<T>(string path) where T : ScriptableObject
    {
        var existing = AssetDatabase.LoadAssetAtPath<T>(path);
        if (existing != null) return existing;

        var asset = ScriptableObject.CreateInstance<T>();
        AssetDatabase.CreateAsset(asset, path);
        return asset;
    }
}
