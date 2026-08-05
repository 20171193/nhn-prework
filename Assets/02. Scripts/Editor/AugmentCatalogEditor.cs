using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// AugmentCatalog 전용 인스펙터.
// 기본 인스펙터는 에셋 참조가 세로로 쭉 늘어선 목록이라 "지금 몇 종이 있고,
// 티어 분포가 어떤지"를 볼 수 없다. 여기서는 다음을 보여준다.
//
//   - 티어별 종 수와 뽑기 비중(AugmentPool의 실제 가중치 기준)
//   - 종류(스탯/스킬) + 티어 + 이름 검색 필터
//   - 항목을 펼치면 설명과 스탯 수정치까지
//   - 비어 있는 항목/중복 등록 같은 실수 경고
//
// 목록 자체를 편집하려면 맨 아래 "원본 목록"을 펼치면 기본 리스트가 나온다.
// 보통은 Tools > Augments > Refresh Catalog로 자동 채우는 쪽이 편하다.
[CustomEditor(typeof(AugmentCatalog))]
public class AugmentCatalogEditor : Editor
{
    enum TypeFilter { All, Stat, Skill }
    enum SortMode { Tier, Name, Type }

    static readonly AugmentTier[] Tiers =
        (AugmentTier[])Enum.GetValues(typeof(AugmentTier));

    const string RefreshMenuPath = "Tools/Augments/Refresh Catalog";

    string search = string.Empty;
    TypeFilter typeFilter = TypeFilter.All;
    int tierMask = ~0;              // 전체 티어 켜짐
    SortMode sortMode = SortMode.Tier;
    bool showRawList;

    readonly HashSet<int> expanded = new HashSet<int>();
    readonly List<AugmentData> filtered = new List<AugmentData>();

    public override void OnInspectorGUI()
    {
        var catalog = (AugmentCatalog)target;

        DrawSummary(catalog);
        EditorGUILayout.Space();

        DrawFilters();
        EditorGUILayout.Space();

        BuildFiltered(catalog);
        DrawList(catalog);

        EditorGUILayout.Space();
        DrawFooter(catalog);
    }

    // ── 요약 ──────────────────────────────────────────────────────────

    void DrawSummary(AugmentCatalog catalog)
    {
        int nulls = 0;
        int noName = 0;
        var counts = new Dictionary<AugmentTier, int>();
        var weights = new Dictionary<AugmentTier, float>();
        var seen = new HashSet<AugmentData>();
        var duplicates = new HashSet<AugmentData>();
        float totalWeight = 0f;

        foreach (var tier in Tiers)
        {
            counts[tier] = 0;
            weights[tier] = 0f;
        }

        foreach (var augment in catalog.Augments)
        {
            if (augment == null)
            {
                nulls++;
                continue;
            }

            if (!seen.Add(augment)) duplicates.Add(augment);
            if (string.IsNullOrEmpty(augment.augmentName)) noName++;

            // 알 수 없는 티어 값이 들어와도 인스펙터가 죽지 않게 방어한다.
            if (!counts.ContainsKey(augment.tier)) continue;

            counts[augment.tier]++;

            float weight = AugmentPool.WeightOf(augment.tier);
            weights[augment.tier] += weight;
            totalWeight += weight;
        }

        EditorGUILayout.LabelField($"증강 {catalog.Count}종", EditorStyles.boldLabel);

        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            foreach (var tier in Tiers)
                DrawTierRow(tier, counts[tier], weights[tier], totalWeight, catalog.Count);

            EditorGUILayout.Space(2f);
            EditorGUILayout.LabelField(
                "비중 = 선택지 1장을 뽑을 때 해당 티어가 나올 확률. " +
                "티어별 가중치(Rare 8 / Epic 4 / Unique 2 / Legendary 1)와 종 수를 함께 반영한다.",
                EditorStyles.miniLabel);
        }

        DrawWarnings(nulls, duplicates.Count, noName);
    }

    void DrawTierRow(AugmentTier tier, int count, float tierWeight, float totalWeight, int totalCount)
    {
        Rect row = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight);

        var chip = new Rect(row.x, row.y + 2f, 4f, row.height - 4f);
        EditorGUI.DrawRect(chip, TierColor(tier));

        var label = new Rect(row.x + 10f, row.y, 78f, row.height);
        EditorGUI.LabelField(label, tier.ToString());

        var countLabel = new Rect(label.xMax, row.y, 46f, row.height);
        EditorGUI.LabelField(countLabel, $"{count}종", EditorStyles.miniLabel);

        // 등록된 증강이 하나도 없으면 확률 자체가 정의되지 않는다.
        float share = totalWeight > 0f ? tierWeight / totalWeight : 0f;

        var barArea = new Rect(countLabel.xMax, row.y + 3f, Mathf.Max(0f, row.width - 226f), row.height - 6f);
        EditorGUI.DrawRect(barArea, BarBackColor);
        if (share > 0f)
        {
            var fill = new Rect(barArea.x, barArea.y, barArea.width * share, barArea.height);
            EditorGUI.DrawRect(fill, TierColor(tier));
        }

        var percent = new Rect(barArea.xMax + 4f, row.y, 84f, row.height);
        EditorGUI.LabelField(percent,
            totalCount > 0 ? $"{share * 100f:0.0}%" : "-",
            EditorStyles.miniLabel);
    }

    static void DrawWarnings(int nulls, int duplicates, int noName)
    {
        if (nulls > 0)
            EditorGUILayout.HelpBox(
                $"비어 있는 항목 {nulls}개. 에셋이 삭제됐을 수 있다. " +
                $"{RefreshMenuPath}로 정리된다.", MessageType.Error);

        if (duplicates > 0)
            EditorGUILayout.HelpBox(
                $"중복 등록된 증강 {duplicates}종. 같은 증강이 여러 번 뽑힐 수 있다.",
                MessageType.Warning);

        if (noName > 0)
            EditorGUILayout.HelpBox(
                $"augmentName이 비어 있는 증강 {noName}종. 선택 UI에 빈 카드로 표시된다.",
                MessageType.Warning);
    }

    // ── 필터 ──────────────────────────────────────────────────────────

    void DrawFilters()
    {
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("종류", GUILayout.Width(40f));
                typeFilter = (TypeFilter)GUILayout.Toolbar((int)typeFilter,
                    new[] { "전체", "스탯", "스킬" });
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("티어", GUILayout.Width(40f));

                foreach (var tier in Tiers)
                {
                    int bit = 1 << (int)tier;
                    bool on = (tierMask & bit) != 0;

                    // 켜진 티어는 티어 색으로 물들여서 한눈에 보이게 한다.
                    Color previous = GUI.backgroundColor;
                    if (on) GUI.backgroundColor = TierColor(tier);

                    if (GUILayout.Toggle(on, tier.ToString(), EditorStyles.miniButton) != on)
                        tierMask ^= bit;

                    GUI.backgroundColor = previous;
                }

                if (GUILayout.Button("전체", EditorStyles.miniButton, GUILayout.Width(40f)))
                    tierMask = ~0;
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("검색", GUILayout.Width(40f));
                search = EditorGUILayout.TextField(search);
                sortMode = (SortMode)EditorGUILayout.EnumPopup(sortMode, GUILayout.Width(70f));
            }
        }
    }

    void BuildFiltered(AugmentCatalog catalog)
    {
        filtered.Clear();

        foreach (var augment in catalog.Augments)
        {
            if (augment == null) continue;
            if ((tierMask & (1 << (int)augment.tier)) == 0) continue;
            if (typeFilter == TypeFilter.Stat && !(augment is StatAugmentData)) continue;
            if (typeFilter == TypeFilter.Skill && !(augment is SkillAugmentData)) continue;
            if (!MatchesSearch(augment)) continue;

            filtered.Add(augment);
        }

        filtered.Sort(Compare);
    }

    bool MatchesSearch(AugmentData augment)
    {
        if (string.IsNullOrEmpty(search)) return true;

        return Contains(augment.name, search)
            || Contains(augment.augmentName, search)
            || Contains(augment.description, search);
    }

    static bool Contains(string text, string needle)
    {
        return !string.IsNullOrEmpty(text)
            && text.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0;
    }

    int Compare(AugmentData a, AugmentData b)
    {
        switch (sortMode)
        {
            case SortMode.Name:
                return string.Compare(DisplayName(a), DisplayName(b), StringComparison.CurrentCulture);

            case SortMode.Type:
                int type = string.CompareOrdinal(TypeLabel(a), TypeLabel(b));
                if (type != 0) return type;
                break;
        }

        // 기본값이자 Type 정렬의 2차 기준. 티어 -> 파일명 순으로,
        // Refresh Catalog가 만드는 순서와 같게 맞춘다.
        if (a.tier != b.tier) return a.tier.CompareTo(b.tier);
        return string.CompareOrdinal(a.name, b.name);
    }

    // ── 목록 ──────────────────────────────────────────────────────────

    void DrawList(AugmentCatalog catalog)
    {
        EditorGUILayout.LabelField(
            filtered.Count == catalog.Count
                ? $"목록 ({filtered.Count}종)"
                : $"목록 ({filtered.Count}종 / 전체 {catalog.Count}종)",
            EditorStyles.boldLabel);

        if (filtered.Count == 0)
        {
            EditorGUILayout.HelpBox(
                catalog.Count == 0
                    ? $"카탈로그가 비어 있다. {RefreshMenuPath}를 실행하면 프로젝트의 증강을 전부 찾아 채운다."
                    : "필터에 걸리는 증강이 없다.",
                MessageType.Info);
            return;
        }

        for (int i = 0; i < filtered.Count; i++)
            DrawRow(filtered[i], i);
    }

    void DrawRow(AugmentData augment, int index)
    {
        int id = augment.GetInstanceID();
        bool isExpanded = expanded.Contains(id);

        Rect row = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight + 4f);
        if (index % 2 == 1) EditorGUI.DrawRect(row, StripeColor);

        var line = new Rect(row.x, row.y + 2f, row.width, EditorGUIUtility.singleLineHeight);

        var foldout = new Rect(line.x, line.y, 14f, line.height);
        if (EditorGUI.Foldout(foldout, isExpanded, GUIContent.none, true) != isExpanded)
        {
            if (isExpanded) expanded.Remove(id);
            else expanded.Add(id);
        }

        var chip = new Rect(line.x + 14f, line.y + 1f, 4f, line.height - 2f);
        EditorGUI.DrawRect(chip, TierColor(augment.tier));

        var button = new Rect(line.xMax - 52f, line.y, 52f, line.height);
        var badge = new Rect(button.x - 50f, line.y, 46f, line.height);
        var name = new Rect(chip.xMax + 6f, line.y, Mathf.Max(0f, badge.x - chip.xMax - 10f), line.height);

        // augmentName이 비어 있으면 최소한 에셋 이름은 보이게 한다.
        string display = string.IsNullOrEmpty(augment.augmentName)
            ? $"({augment.name})"
            : augment.augmentName;
        EditorGUI.LabelField(name, new GUIContent(display, augment.name));

        EditorGUI.LabelField(badge, TypeLabel(augment), EditorStyles.miniLabel);

        // Selection을 바꾸면 이 인스펙터 자체가 닫혀버리므로 핑만 보낸다.
        // 프로젝트 창에서 해당 에셋이 하이라이트되고, 카탈로그는 그대로 열려 있다.
        if (GUI.Button(button, "찾기", EditorStyles.miniButton))
            EditorGUIUtility.PingObject(augment);

        if (isExpanded) DrawDetail(augment);
    }

    void DrawDetail(AugmentData augment)
    {
        using (new EditorGUI.IndentLevelScope())
        {
            EditorGUILayout.LabelField("티어", augment.tier.ToString());
            EditorGUILayout.LabelField("설명",
                string.IsNullOrEmpty(augment.description) ? "(없음)" : augment.description,
                EditorStyles.wordWrappedLabel);

            if (augment is StatAugmentData stat) DrawModifiers(stat);
            else if (augment is SkillAugmentData) EditorGUILayout.LabelField("효과", "스킬 증강 (Apply 구현 필요)");

            EditorGUILayout.LabelField("경로", AssetDatabase.GetAssetPath(augment), EditorStyles.miniLabel);
            EditorGUILayout.Space(2f);
        }
    }

    static void DrawModifiers(StatAugmentData stat)
    {
        if (stat.modifiers == null || stat.modifiers.Count == 0)
        {
            EditorGUILayout.LabelField("수정치", "(없음 — 아무 효과도 없다)");
            return;
        }

        EditorGUILayout.LabelField("수정치", $"{stat.modifiers.Count}개");

        using (new EditorGUI.IndentLevelScope())
        {
            foreach (var modifier in stat.modifiers)
            {
                if (modifier == null) continue;

                // Add는 부호를 살려서 +1 / -1로, Multiply는 x1.25로 읽히게 한다.
                string value = modifier.operation == StatOperation.Add
                    ? $"{modifier.value:+0.##;-0.##;0}"
                    : $"x{modifier.value:0.##}";

                EditorGUILayout.LabelField(modifier.statType.ToString(), value);
            }
        }
    }

    // ── 하단 ──────────────────────────────────────────────────────────

    void DrawFooter(AugmentCatalog catalog)
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("카탈로그 갱신 (프로젝트 전체 스캔)"))
                EditorApplication.ExecuteMenuItem(RefreshMenuPath);

            using (new EditorGUI.DisabledScope(filtered.Count == 0))
            {
                if (GUILayout.Button("펼치기", GUILayout.Width(60f)))
                    foreach (var augment in filtered) expanded.Add(augment.GetInstanceID());

                if (GUILayout.Button("접기", GUILayout.Width(60f)))
                    expanded.Clear();
            }
        }

        showRawList = EditorGUILayout.Foldout(showRawList, "원본 목록 (직접 편집)", true);
        if (!showRawList) return;

        serializedObject.Update();
        EditorGUILayout.PropertyField(
            serializedObject.FindProperty(nameof(AugmentCatalog.Augments)), true);
        serializedObject.ApplyModifiedProperties();
    }

    // ── 표시 도우미 ───────────────────────────────────────────────────

    static string DisplayName(AugmentData augment)
    {
        return string.IsNullOrEmpty(augment.augmentName) ? augment.name : augment.augmentName;
    }

    // AugmentData를 상속한 새 종류가 생겨도 클래스 이름으로 표시된다.
    static string TypeLabel(AugmentData augment)
    {
        if (augment is StatAugmentData) return "스탯";
        if (augment is SkillAugmentData) return "스킬";

        string typeName = augment.GetType().Name;
        return typeName.EndsWith("AugmentData", StringComparison.Ordinal)
            ? typeName.Substring(0, typeName.Length - "AugmentData".Length)
            : typeName;
    }

    static Color TierColor(AugmentTier tier)
    {
        switch (tier)
        {
            case AugmentTier.Rare:      return new Color(0.35f, 0.62f, 0.92f);
            case AugmentTier.Epic:      return new Color(0.68f, 0.45f, 0.90f);
            case AugmentTier.Unique:    return new Color(0.95f, 0.68f, 0.25f);
            case AugmentTier.Legendary: return new Color(0.92f, 0.36f, 0.36f);
            default:                    return Color.gray;
        }
    }

    static Color StripeColor => EditorGUIUtility.isProSkin
        ? new Color(1f, 1f, 1f, 0.03f)
        : new Color(0f, 0f, 0f, 0.03f);

    static Color BarBackColor => EditorGUIUtility.isProSkin
        ? new Color(1f, 1f, 1f, 0.08f)
        : new Color(0f, 0f, 0f, 0.08f);
}
