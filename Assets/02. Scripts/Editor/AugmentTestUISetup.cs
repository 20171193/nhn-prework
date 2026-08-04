using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Tools > Combat Prototype > Add Augment Test UI 실행 시
// 우측 상단에 현재 스탯을 보여주는 텍스트와, 증강 4종을 즉시 적용해볼 수 있는
// 버튼을 씬에 배치한다. 이미 되어 있으면 건너뛰므로 여러 번 실행해도 안전하다.
public static class AugmentTestUISetup
{
    const string CanvasName = "AugmentTestCanvas";

    static readonly (string label, string method)[] AugmentButtons =
    {
        ("Projectile Count +1", nameof(AugmentTestUI.AddProjectileCount)),
        ("Projectile Speed +2", nameof(AugmentTestUI.AddProjectileSpeed)),
        ("Attack Speed +0.5", nameof(AugmentTestUI.AddAttackRate)),
        ("Move Speed +5", nameof(AugmentTestUI.AddMoveSpeed)),
    };

    [MenuItem("Tools/Combat Prototype/Add Augment Test UI")]
    static void Build()
    {
        var player = GameObject.Find("Player");
        if (player == null)
        {
            Debug.LogWarning("씬에서 Player를 찾을 수 없습니다.");
            return;
        }

        var combatContext = player.GetComponent<PlayerCombatContext>();
        if (combatContext == null)
        {
            Debug.LogWarning("Player에 PlayerCombatContext가 없습니다. 먼저 'Add Core Combat Stats'를 실행하세요.");
            return;
        }

        var font = PrototypeFonts.GetDefaultFont();

        var canvas = EnsureCanvas();
        var statsText = EnsureStatsText(canvas.transform, font);
        var selectedAugmentsText = EnsureSelectedAugmentsText(canvas.transform, font);
        var augmentUI = EnsureAugmentTestUIComponent(canvas.gameObject, combatContext, statsText, selectedAugmentsText);
        EnsureButtons(canvas.transform, font, augmentUI);

        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Selection.activeGameObject = canvas.gameObject;
        Debug.Log("증강 테스트 UI 세팅 완료. Ctrl+S로 씬을 저장하세요.");
    }

    static Canvas EnsureCanvas()
    {
        var existing = GameObject.Find(CanvasName);
        if (existing != null) return existing.GetComponent<Canvas>();

        var canvasObj = new GameObject(CanvasName, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = canvasObj.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        var scaler = canvasObj.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);

        return canvas;
    }

    static TMP_Text EnsureStatsText(Transform canvasTransform, TMP_FontAsset font)
    {
        var existing = canvasTransform.Find("StatsText");
        if (existing != null) return existing.GetComponent<TMP_Text>();

        var textObj = new GameObject("StatsText", typeof(RectTransform));
        textObj.transform.SetParent(canvasTransform, false);

        var rect = textObj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(1, 1);
        rect.anchorMax = new Vector2(1, 1);
        rect.pivot = new Vector2(1, 1);
        rect.anchoredPosition = new Vector2(-20, -20);
        rect.sizeDelta = new Vector2(300, 160);

        var text = textObj.AddComponent<TextMeshProUGUI>();
        text.font = font;
        text.fontSize = 22;
        text.alignment = TextAlignmentOptions.TopRight;
        text.color = Color.white;
        text.text = "스탯 로딩 중...";

        return text;
    }

    static TMP_Text EnsureSelectedAugmentsText(Transform canvasTransform, TMP_FontAsset font)
    {
        var existing = canvasTransform.Find("SelectedAugmentsText");
        if (existing != null) return existing.GetComponent<TMP_Text>();

        var textObj = new GameObject("SelectedAugmentsText", typeof(RectTransform));
        textObj.transform.SetParent(canvasTransform, false);

        var rect = textObj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(1, 1);
        rect.anchorMax = new Vector2(1, 1);
        rect.pivot = new Vector2(1, 1);
        rect.anchoredPosition = new Vector2(-20, -190);
        rect.sizeDelta = new Vector2(300, 140);

        var text = textObj.AddComponent<TextMeshProUGUI>();
        text.font = font;
        text.fontSize = 18;
        text.alignment = TextAlignmentOptions.TopRight;
        text.color = new Color(1f, 0.85f, 0.4f);
        text.text = "";

        return text;
    }

    static AugmentTestUI EnsureAugmentTestUIComponent(GameObject canvasObj, PlayerCombatContext combatContext, TMP_Text statsText, TMP_Text selectedAugmentsText)
    {
        var augmentUI = canvasObj.GetComponent<AugmentTestUI>();
        if (augmentUI == null)
            augmentUI = canvasObj.AddComponent<AugmentTestUI>();

        augmentUI.combatContext = combatContext;
        augmentUI.statsText = statsText;
        augmentUI.selectedAugmentsText = selectedAugmentsText;

        return augmentUI;
    }

    static void EnsureButtons(Transform canvasTransform, TMP_FontAsset font, AugmentTestUI augmentUI)
    {
        var container = canvasTransform.Find("AugmentButtons");
        if (container == null)
        {
            var containerObj = new GameObject("AugmentButtons", typeof(RectTransform));
            containerObj.transform.SetParent(canvasTransform, false);

            var containerRect = containerObj.GetComponent<RectTransform>();
            containerRect.anchorMin = new Vector2(1, 1);
            containerRect.anchorMax = new Vector2(1, 1);
            containerRect.pivot = new Vector2(1, 1);
            containerRect.anchoredPosition = new Vector2(-20, -340);
            containerRect.sizeDelta = new Vector2(220, 200);

            var layout = containerObj.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 8;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            container = containerObj.transform;
        }

        for (int i = 0; i < AugmentButtons.Length; i++)
        {
            var (label, method) = AugmentButtons[i];
            var buttonName = $"Button_{method}";

            var existingButton = container.Find(buttonName);
            if (existingButton != null)
            {
                var existingLabel = existingButton.Find("Label")?.GetComponent<TMP_Text>();
                if (existingLabel != null) existingLabel.text = label;
                continue;
            }

            CreateButton(container, buttonName, label, font, augmentUI, method);
        }
    }

    static void CreateButton(Transform parent, string name, string label, TMP_FontAsset font, AugmentTestUI augmentUI, string methodName)
    {
        var buttonObj = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
        buttonObj.transform.SetParent(parent, false);

        var layoutElement = buttonObj.GetComponent<LayoutElement>();
        layoutElement.preferredHeight = 36;

        var image = buttonObj.GetComponent<Image>();
        image.color = new Color(0.15f, 0.15f, 0.15f, 0.85f);

        var textObj = new GameObject("Label", typeof(RectTransform));
        textObj.transform.SetParent(buttonObj.transform, false);
        var textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        var text = textObj.AddComponent<TextMeshProUGUI>();
        text.font = font;
        text.fontSize = 16;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;
        text.text = label;

        var button = buttonObj.GetComponent<Button>();
        var action = System.Delegate.CreateDelegate(typeof(UnityEngine.Events.UnityAction), augmentUI, methodName) as UnityEngine.Events.UnityAction;
        UnityEventTools.AddPersistentListener(button.onClick, action);
    }
}
