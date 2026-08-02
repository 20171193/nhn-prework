using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Tools > Combat Prototype > Add Stat Menu 실행 시
// 화면 우측에 공격 속도/투사체 속도/이동 속도를 -/+ 버튼으로 조절하는 스텟 메뉴를 배치한다.
public static class StatMenuSceneSetup
{
    [MenuItem("Tools/Combat Prototype/Add Stat Menu")]
    static void Build()
    {
        var player = GameObject.Find("Player");
        var controller = player != null ? player.GetComponent<PlayerController>() : null;
        if (controller == null)
        {
            Debug.LogWarning("씬에서 PlayerController를 찾을 수 없습니다.");
            return;
        }

        if (GameObject.Find("StatMenuCanvas") != null)
        {
            Debug.LogWarning("StatMenuCanvas가 이미 씬에 있습니다.");
            return;
        }

        var font = PrototypeFonts.GetDefaultFont();
        if (font == null) return;

        EnsureEventSystem();

        var canvasObj = new GameObject("StatMenuCanvas", typeof(RectTransform));
        var canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);
        canvasObj.AddComponent<GraphicRaycaster>();

        var panel = CreatePanel(canvasObj.transform);
        var statMenu = canvasObj.AddComponent<StatMenuUI>();
        statMenu.player = controller;

        var (attackLabel, attackMinus, attackPlus) = CreateStatRow(panel.transform, "AttackRateRow", font);
        statMenu.attackRateLabel = attackLabel;
        UnityEventTools.AddPersistentListener(attackMinus.onClick, statMenu.DecreaseAttackRate);
        UnityEventTools.AddPersistentListener(attackPlus.onClick, statMenu.IncreaseAttackRate);

        var (projLabel, projMinus, projPlus) = CreateStatRow(panel.transform, "ProjectileSpeedRow", font);
        statMenu.projectileSpeedLabel = projLabel;
        UnityEventTools.AddPersistentListener(projMinus.onClick, statMenu.DecreaseProjectileSpeed);
        UnityEventTools.AddPersistentListener(projPlus.onClick, statMenu.IncreaseProjectileSpeed);

        var (moveLabel, moveMinus, movePlus) = CreateStatRow(panel.transform, "MoveSpeedRow", font);
        statMenu.moveSpeedLabel = moveLabel;
        UnityEventTools.AddPersistentListener(moveMinus.onClick, statMenu.DecreaseMoveSpeed);
        UnityEventTools.AddPersistentListener(movePlus.onClick, statMenu.IncreaseMoveSpeed);

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Selection.activeGameObject = canvasObj;
        Debug.Log("스텟 메뉴 배치 완료. Ctrl+S로 씬을 저장하세요.");
    }

    static void EnsureEventSystem()
    {
        if (Object.FindFirstObjectByType<EventSystem>() != null) return;

        var es = new GameObject("EventSystem", typeof(EventSystem));
        es.AddComponent<StandaloneInputModule>();
    }

    static GameObject CreatePanel(Transform parent)
    {
        var panel = new GameObject("Panel", typeof(RectTransform));
        panel.transform.SetParent(parent, false);

        var rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(1f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(1f, 1f);
        rect.sizeDelta = new Vector2(300f, 150f);
        rect.anchoredPosition = new Vector2(-20f, -20f);

        var image = panel.AddComponent<Image>();
        image.color = new Color(0f, 0f, 0f, 0.5f);

        var layout = panel.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(10, 10, 10, 10);
        layout.spacing = 8f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        return panel;
    }

    static (TMP_Text label, Button minus, Button plus) CreateStatRow(Transform parent, string name, TMP_FontAsset font)
    {
        var row = new GameObject(name, typeof(RectTransform));
        row.transform.SetParent(parent, false);

        var rowLayoutElement = row.AddComponent<LayoutElement>();
        rowLayoutElement.preferredHeight = 32f;

        var rowLayout = row.AddComponent<HorizontalLayoutGroup>();
        rowLayout.spacing = 4f;
        rowLayout.childControlWidth = true;
        rowLayout.childControlHeight = true;
        rowLayout.childForceExpandWidth = false;
        rowLayout.childForceExpandHeight = true;

        var label = CreateLabel(row.transform, "Label", TextAlignmentOptions.MidlineLeft, font);
        label.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

        var minus = CreateButton(row.transform, "MinusButton", "-", font);
        var plus = CreateButton(row.transform, "PlusButton", "+", font);

        return (label, minus, plus);
    }

    static TMP_Text CreateLabel(Transform parent, string name, TextAlignmentOptions alignment, TMP_FontAsset font)
    {
        var obj = new GameObject(name, typeof(RectTransform));
        obj.transform.SetParent(parent, false);

        var text = obj.AddComponent<TextMeshProUGUI>();
        text.font = font;
        text.fontSize = 16f;
        text.alignment = alignment;
        text.color = Color.white;
        text.text = name;
        return text;
    }

    static Button CreateButton(Transform parent, string name, string label, TMP_FontAsset font)
    {
        var obj = new GameObject(name, typeof(RectTransform));
        obj.transform.SetParent(parent, false);

        var layoutElement = obj.AddComponent<LayoutElement>();
        layoutElement.preferredWidth = 28f;
        layoutElement.preferredHeight = 28f;

        var image = obj.AddComponent<Image>();
        image.color = new Color(1f, 1f, 1f, 0.15f);

        var button = obj.AddComponent<Button>();
        button.targetGraphic = image;

        var text = CreateLabel(obj.transform, "Text", TextAlignmentOptions.Center, font);
        text.text = label;
        var textRect = text.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        return button;
    }
}
