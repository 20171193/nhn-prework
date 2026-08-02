using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Tools > Combat Prototype 메뉴로 모드 안내 라벨(TMP, 화면 좌상단)과
// 조준 방향 표시(Player 자식, 회전을 그대로 물려받는 얇은 막대)를 씬에 배치한다.
public static class PrototypeHudSetup
{
    [MenuItem("Tools/Combat Prototype/Add Mode Label")]
    static void BuildModeLabel()
    {
        var player = GameObject.Find("Player");
        var controller = player != null ? player.GetComponent<PlayerController>() : null;
        if (controller == null)
        {
            Debug.LogWarning("씬에서 PlayerController를 찾을 수 없습니다.");
            return;
        }

        if (GameObject.Find("ModeLabelCanvas") != null)
        {
            Debug.LogWarning("ModeLabelCanvas가 이미 씬에 있습니다.");
            return;
        }

        var font = PrototypeFonts.GetDefaultFont();
        if (font == null) return;

        var canvasObj = new GameObject("ModeLabelCanvas", typeof(RectTransform));
        var canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);

        var labelObj = new GameObject("ModeLabel", typeof(RectTransform));
        labelObj.transform.SetParent(canvasObj.transform, false);

        var rect = labelObj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.sizeDelta = new Vector2(560f, 60f);
        rect.anchoredPosition = new Vector2(20f, -20f);

        var text = labelObj.AddComponent<TextMeshProUGUI>();
        text.font = font;
        text.fontSize = 18f;
        text.alignment = TextAlignmentOptions.TopLeft;
        text.color = Color.white;
        text.text = "Mode: 1 - AutoAimAutoFire";

        controller.modeLabel = text;

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Selection.activeGameObject = canvasObj;
        Debug.Log("모드 안내 라벨 배치 완료. Ctrl+S로 씬을 저장하세요.");
    }

    [MenuItem("Tools/Combat Prototype/Add Aim Indicator")]
    static void BuildAimIndicator()
    {
        var player = GameObject.Find("Player");
        if (player == null)
        {
            Debug.LogWarning("씬에서 Player를 찾을 수 없습니다.");
            return;
        }

        if (player.transform.Find("AimIndicator") != null)
        {
            Debug.LogWarning("Player 아래에 AimIndicator가 이미 있습니다.");
            return;
        }

        var indicator = GameObject.CreatePrimitive(PrimitiveType.Cube);
        indicator.name = "AimIndicator";
        Object.DestroyImmediate(indicator.GetComponent<Collider>());
        indicator.transform.SetParent(player.transform, false);
        indicator.transform.localPosition = new Vector3(0f, 1f, 0f);
        indicator.transform.localScale = new Vector3(0.08f, 0.7f, 0.08f);
        indicator.GetComponent<Renderer>().material.color = Color.red;

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Selection.activeGameObject = indicator;
        Debug.Log("에임 인디케이터 배치 완료. Ctrl+S로 씬을 저장하세요.");
    }
}
