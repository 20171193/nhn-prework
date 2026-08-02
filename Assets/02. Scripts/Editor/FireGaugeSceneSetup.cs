using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Tools > Combat Prototype > Add Fire Gauge To Player 실행 시
// 현재 씬에 발사 딜레이 게이지(Slider)를 배치하고 Player를 따라다니도록 연결한다.
// Player의 자식이 아니라 별도 오브젝트로 두어 플레이어 회전에 영향받지 않는다.
public static class FireGaugeSceneSetup
{
    static readonly Vector3 GaugeOffset = new Vector3(0f, 1.2f, 0f);
    static readonly Vector2 GaugeSize = new Vector2(1.2f, 0.16f);

    [MenuItem("Tools/Combat Prototype/Add Fire Gauge To Player")]
    static void Build()
    {
        var player = GameObject.Find("Player");
        if (player == null)
        {
            Debug.LogWarning("씬에서 Player를 찾을 수 없습니다.");
            return;
        }

        if (GameObject.Find("FireGaugeCanvas") != null)
        {
            Debug.LogWarning("FireGaugeCanvas가 이미 씬에 있습니다.");
            return;
        }

        var canvasObj = new GameObject("FireGaugeCanvas", typeof(RectTransform));
        canvasObj.transform.position = player.transform.position + GaugeOffset;

        var canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        var canvasRect = canvasObj.GetComponent<RectTransform>();
        canvasRect.sizeDelta = GaugeSize;
        canvasRect.localScale = Vector3.one;

        CreateStretchedImage("Background", canvasObj.transform, new Color(0.15f, 0.15f, 0.15f, 0.85f));

        var fillArea = new GameObject("Fill Area", typeof(RectTransform));
        fillArea.transform.SetParent(canvasObj.transform, false);
        StretchFull(fillArea.GetComponent<RectTransform>());

        var fill = CreateStretchedImage("Fill", fillArea.transform, Color.yellow);

        var slider = canvasObj.AddComponent<Slider>();
        slider.interactable = false;
        slider.transition = Selectable.Transition.None;
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.wholeNumbers = false;
        slider.fillRect = fill.rectTransform;
        slider.targetGraphic = null;
        slider.value = 1f;

        var gauge = canvasObj.AddComponent<FireGaugeUI>();
        gauge.target = player.transform;
        gauge.offset = GaugeOffset;
        gauge.slider = slider;
        gauge.fillImage = fill;

        var controller = player.GetComponent<PlayerController>();
        if (controller != null)
            controller.fireGauge = gauge;

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Selection.activeGameObject = canvasObj;
        Debug.Log("발사 게이지 배치 완료. Ctrl+S로 씬을 저장하세요.");
    }

    static Image CreateStretchedImage(string name, Transform parent, Color color)
    {
        var obj = new GameObject(name, typeof(RectTransform));
        obj.transform.SetParent(parent, false);
        StretchFull(obj.GetComponent<RectTransform>());

        var image = obj.AddComponent<Image>();
        image.color = color;
        return image;
    }

    static void StretchFull(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
