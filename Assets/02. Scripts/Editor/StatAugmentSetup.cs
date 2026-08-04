using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// 증강 에셋 관리용 에디터 도구. 메뉴 두 개를 제공한다.
//
// Tools > Augments > Create Stat Augments
//   기본 스탯 증강 13종(티어별 3종 이상)을 만들고 카탈로그까지 갱신한다.
//   이미 있는 에셋은 값을 덮어쓰지 않으므로, 여러 번 실행해도
//   인스펙터에서 손으로 튜닝한 수치가 날아가지 않는다.
//   수치는 전투 기본값(이동 5, 공격속도 2, 데미지 10, 발사체 속도 12, 개수 1) 기준이다.
//
// Tools > Augments > Refresh Catalog
//   프로젝트 전체를 훑어서 AugmentCatalog를 다시 채운다.
//   손으로 만든 증강도 자동으로 잡히므로 카탈로그에 등록하는 것을 잊어도 된다.
public static class StatAugmentSetup
{
    const string AugmentDataPath = "Assets/04. Data/Augments/";
    const string StatAugmentFolder = AugmentDataPath + "Stat Augments";
    const string CatalogPath = AugmentDataPath + "AugmentCatalog.asset";

    [MenuItem("Tools/Augments/Create Stat Augments")]
    static void Build()
    {
        EnsureFolder();

        // Rare - 스탯 하나만 무난하게 올린다. 가장 자주 뽑힌다.
        Register("SwiftFeet", "신속한 발놀림", "이동 속도 +1", AugmentTier.Rare,
            new StatModifier(StatType.MoveSpeed, StatOperation.Add, 1f));

        Register("SharpenedRounds", "예리한 탄자", "발사체 데미지 +4", AugmentTier.Rare,
            new StatModifier(StatType.ProjectileDamage, StatOperation.Add, 4f));

        Register("RapidFire", "속사", "공격 속도 x1.25", AugmentTier.Rare,
            new StatModifier(StatType.AttackRate, StatOperation.Multiply, 1.25f));

        Register("HighVelocity", "고속탄", "발사체 속도 +4", AugmentTier.Rare,
            new StatModifier(StatType.ProjectileSpeed, StatOperation.Add, 4f));

        // Epic - 스탯 두 개를 건드리거나, 발사체 개수처럼 체감이 큰 것을 준다.
        Register("DoubleShot", "이중 사격", "발사체 +1발. 부채꼴로 퍼져 나간다.", AugmentTier.Epic,
            new StatModifier(StatType.ProjectileCount, StatOperation.Add, 1f));

        Register("Frenzy", "광폭화", "공격 속도 x1.4, 이동 속도 +0.5", AugmentTier.Epic,
            new StatModifier(StatType.AttackRate, StatOperation.Multiply, 1.4f),
            new StatModifier(StatType.MoveSpeed, StatOperation.Add, 0.5f));

        Register("HeavyRounds", "중화기", "데미지 x1.5. 대신 발사체가 느려진다.", AugmentTier.Epic,
            new StatModifier(StatType.ProjectileDamage, StatOperation.Multiply, 1.5f),
            new StatModifier(StatType.ProjectileSpeed, StatOperation.Multiply, 0.8f));

        // Unique - 확실한 대가를 치르고 한쪽으로 특화시킨다.
        Register("Overdrive", "폭주", "공격 속도 x1.8. 대신 데미지 x0.7", AugmentTier.Unique,
            new StatModifier(StatType.AttackRate, StatOperation.Multiply, 1.8f),
            new StatModifier(StatType.ProjectileDamage, StatOperation.Multiply, 0.7f));

        Register("Marksman", "저격수", "데미지 x2, 발사체 속도 x1.5. 대신 공격 속도 x0.6", AugmentTier.Unique,
            new StatModifier(StatType.ProjectileDamage, StatOperation.Multiply, 2f),
            new StatModifier(StatType.ProjectileSpeed, StatOperation.Multiply, 1.5f),
            new StatModifier(StatType.AttackRate, StatOperation.Multiply, 0.6f));

        Register("Acrobat", "곡예사", "이동 속도 x1.6. 대신 데미지 x0.8", AugmentTier.Unique,
            new StatModifier(StatType.MoveSpeed, StatOperation.Multiply, 1.6f),
            new StatModifier(StatType.ProjectileDamage, StatOperation.Multiply, 0.8f));

        // Legendary - 빌드 자체를 바꾼다. 가장 드물게 뽑힌다.
        // 셋이 서로 다른 방향(다탄/한방/종합)이라 뭘 뽑느냐에 따라 남은 라운드 운영이 갈린다.
        Register("BulletHell", "탄막", "발사체 +2발. 대신 데미지 x0.7, 이동 속도 x0.9", AugmentTier.Legendary,
            new StatModifier(StatType.ProjectileCount, StatOperation.Add, 2f),
            new StatModifier(StatType.ProjectileDamage, StatOperation.Multiply, 0.7f),
            new StatModifier(StatType.MoveSpeed, StatOperation.Multiply, 0.9f));

        Register("OneShot", "일격필살", "데미지 x3, 발사체 속도 x2. 대신 공격 속도 x0.35", AugmentTier.Legendary,
            new StatModifier(StatType.ProjectileDamage, StatOperation.Multiply, 3f),
            new StatModifier(StatType.ProjectileSpeed, StatOperation.Multiply, 2f),
            new StatModifier(StatType.AttackRate, StatOperation.Multiply, 0.35f));

        Register("Awakening", "각성", "이동 +1, 데미지 +5, 발사체 속도 +3, 공격 속도 x1.2", AugmentTier.Legendary,
            new StatModifier(StatType.MoveSpeed, StatOperation.Add, 1f),
            new StatModifier(StatType.ProjectileDamage, StatOperation.Add, 5f),
            new StatModifier(StatType.ProjectileSpeed, StatOperation.Add, 3f),
            new StatModifier(StatType.AttackRate, StatOperation.Multiply, 1.2f));

        AssetDatabase.SaveAssets();
        RefreshCatalog();
    }

    // 프로젝트 전체에서 AugmentData를 상속한 에셋을 전부 찾아 카탈로그를 다시 채운다.
    // StatAugmentData든 SkillAugmentData든 종류를 가리지 않고,
    // 지워진 에셋이 남긴 빈 항목도 이 과정에서 같이 정리된다.
    // 순서는 티어 -> 파일명으로 고정해서 인스펙터에서 보기 편하게 한다.
    [MenuItem("Tools/Augments/Refresh Catalog")]
    static void RefreshCatalog()
    {
        EnsureFolder();

        var catalog = GetOrCreateAsset<AugmentCatalog>(CatalogPath);
        int before = catalog.Augments.Count;

        var found = new List<AugmentData>();
        foreach (string guid in AssetDatabase.FindAssets($"t:{nameof(AugmentData)}"))
        {
            var augment = AssetDatabase.LoadAssetAtPath<AugmentData>(AssetDatabase.GUIDToAssetPath(guid));
            if (augment != null) found.Add(augment);
        }

        found.Sort((a, b) => a.tier != b.tier
            ? a.tier.CompareTo(b.tier)
            : string.CompareOrdinal(a.name, b.name));

        catalog.Augments = found;
        EditorUtility.SetDirty(catalog);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"AugmentCatalog 갱신: {before}종 -> {found.Count}종 ({CatalogPath})");
    }

    // 에셋이 없을 때만 만들고 수치를 채운다. 이미 있으면 그대로 둔다.
    static void Register(string fileName, string augmentName, string description,
        AugmentTier tier, params StatModifier[] modifiers)
    {
        string path = $"{StatAugmentFolder}/{fileName}.asset";
        if (AssetDatabase.LoadAssetAtPath<StatAugmentData>(path) != null) return;

        var augment = ScriptableObject.CreateInstance<StatAugmentData>();
        augment.augmentName = augmentName;
        augment.description = description;
        augment.tier = tier;
        augment.modifiers = new List<StatModifier>(modifiers);
        AssetDatabase.CreateAsset(augment, path);
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

    static void EnsureFolder()
    {
        if (!AssetDatabase.IsValidFolder("Assets/04. Data"))
            AssetDatabase.CreateFolder("Assets", "04. Data");

        if (!AssetDatabase.IsValidFolder(StatAugmentFolder))
            AssetDatabase.CreateFolder("Assets/04. Data", "Augments");
    }
}
