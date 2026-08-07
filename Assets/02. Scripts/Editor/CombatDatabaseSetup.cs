using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// 전투 데이터 DB(Projectile/Weapon/ThrowableWeapon) 관리용 에디터 도구.
//
// Tools > Combat > Setup Databases
//   Assets/07. Data/Combat/Resources/ 아래에 Database 에셋 3종을 없으면 만들고
//   (Resources.Load로 런타임에 정적 로딩하기 위해 Resources 폴더에 둔다 - Bootstrap 참고),
//   기존 Stat_Weapon_Gun/Stat_Projectile_Gun에 id가 비어있으면(0이면) 채우고,
//   투척무기 플레이스홀더 3종(연막탄/수류탄/섬광탄)을 없으면 만든 뒤,
//   프로젝트 전체를 훑어 각 Database의 entries를 다시 채운다.
//   이미 채워진 값은 덮어쓰지 않으므로 여러 번 실행해도 안전하다.
public static class CombatDatabaseSetup
{
    const string DatabaseFolder = "Assets/07. Data/Combat/Resources";
    const string ThrowableWeaponFolder = "Assets/07. Data/Combat/Database/ThrowableWeapons";

    const string ProjectileDatabasePath = DatabaseFolder + "/ProjectileDatabase.asset";
    const string WeaponDatabasePath = DatabaseFolder + "/WeaponDatabase.asset";
    const string ThrowableWeaponDatabasePath = DatabaseFolder + "/ThrowableWeaponDatabase.asset";

    const string WeaponGunPath = "Assets/07. Data/Combat/Stat/Stat_Weapon_Gun.asset";
    const string ProjectileGunPath = "Assets/07. Data/Combat/Stat/Stat_Projectile_Gun.asset";

    [MenuItem("Tools/Combat/Setup Databases")]
    static void Setup()
    {
        EnsureFolders();

        AssignWeaponIdIfEmpty();
        AssignProjectileIdIfEmpty();

        // effectValue 의미: Grenade=데미지, Flash/Smoke=지속시간(초). 임의 기본값 - 기획 조정 예정.
        RegisterThrowableWeapon("Smoke", 30001, triggerDelay: 0.5f, effectRadius: 3f, effectValue: 5f);
        RegisterThrowableWeapon("Grenade", 30002, triggerDelay: 1.5f, effectRadius: 2.5f, effectValue: 30f);
        RegisterThrowableWeapon("Flash", 30003, triggerDelay: 1f, effectRadius: 4f, effectValue: 2f);

        var projectileDb = GetOrCreateAsset<ProjectileDatabase>(ProjectileDatabasePath);
        var weaponDb = GetOrCreateAsset<WeaponDatabase>(WeaponDatabasePath);
        var throwableWeaponDb = GetOrCreateAsset<ThrowableWeaponDatabase>(ThrowableWeaponDatabasePath);

        Refresh<ProjectileDatabase, ProjectileData>(projectileDb);
        Refresh<WeaponDatabase, WeaponData>(weaponDb);
        Refresh<ThrowableWeaponDatabase, ThrowableWeaponData>(throwableWeaponDb);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("전투 데이터 DB 세팅 완료.");
    }

    static void AssignWeaponIdIfEmpty()
    {
        var data = AssetDatabase.LoadAssetAtPath<WeaponData>(WeaponGunPath);
        if (data == null || data.id != 0) return;

        data.id = 20001;
        EditorUtility.SetDirty(data);
    }

    static void AssignProjectileIdIfEmpty()
    {
        var data = AssetDatabase.LoadAssetAtPath<ProjectileData>(ProjectileGunPath);
        if (data == null || data.id != 0) return;

        data.id = 10001;
        EditorUtility.SetDirty(data);
    }

    // 이미 있는 에셋은 건드리지 않는다(아트/기획이 손으로 채운 prefab·icon·수치를 지키기 위함).
    static void RegisterThrowableWeapon(string fileName, int id, float triggerDelay, float effectRadius, float effectValue)
    {
        string path = $"{ThrowableWeaponFolder}/{fileName}.asset";
        if (AssetDatabase.LoadAssetAtPath<ThrowableWeaponData>(path) != null) return;

        var weapon = ScriptableObject.CreateInstance<ThrowableWeaponData>();
        weapon.id = id;
        weapon.triggerDelay = triggerDelay;
        weapon.effectRadius = effectRadius;
        weapon.effectValue = effectValue;
        AssetDatabase.CreateAsset(weapon, path);
    }

    // 프로젝트 전체에서 TEntry 타입 에셋을 찾아 database.entries를 다시 채운다.
    static void Refresh<TDb, TEntry>(TDb database)
        where TDb : Database<TEntry>
        where TEntry : Object, IHasId
    {
        var found = new List<TEntry>();
        foreach (string guid in AssetDatabase.FindAssets($"t:{typeof(TEntry).Name}"))
        {
            var entry = AssetDatabase.LoadAssetAtPath<TEntry>(AssetDatabase.GUIDToAssetPath(guid));
            if (entry != null) found.Add(entry);
        }

        found.Sort((a, b) => a.Id.CompareTo(b.Id));

        database.entries = found;
        EditorUtility.SetDirty(database);
    }

    static T GetOrCreateAsset<T>(string path) where T : ScriptableObject
    {
        var asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset == null)
        {
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
        }
        return asset;
    }

    static void EnsureFolders()
    {
        if (!AssetDatabase.IsValidFolder(DatabaseFolder))
            AssetDatabase.CreateFolder("Assets/07. Data/Combat", "Resources");

        if (!AssetDatabase.IsValidFolder("Assets/07. Data/Combat/Database"))
            AssetDatabase.CreateFolder("Assets/07. Data/Combat", "Database");

        if (!AssetDatabase.IsValidFolder(ThrowableWeaponFolder))
            AssetDatabase.CreateFolder("Assets/07. Data/Combat/Database", "ThrowableWeapons");
    }
}
