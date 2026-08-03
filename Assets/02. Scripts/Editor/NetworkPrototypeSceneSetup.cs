using System.Collections.Generic;
using Photon.Pun;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Tools > Combat Prototype > Add Network 1v1 Setup 실행 시
// 1:1 매칭/스폰용 오브젝트(스폰 포인트 2개, 상태 라벨, NetworkManager)를 배치하고
// PhotonNetwork.Instantiate가 참조할 NetworkPlayer 프리팹을 Resources 폴더에 만든다.
public static class NetworkPrototypeSceneSetup
{
    const string PrefabPath = "Assets/02. Scripts/Network/Resources/NetworkPlayer.prefab";

    [MenuItem("Tools/Combat Prototype/Add Network 1v1 Setup")]
    static void Build()
    {
        if (GameObject.Find("CombatNetworkManager") != null)
        {
            Debug.LogWarning("CombatNetworkManager가 이미 씬에 있습니다.");
            return;
        }

        var font = PrototypeFonts.GetDefaultFont();
        if (font == null) return;

        var networkPlayerPrefab = GetOrCreateNetworkPlayerPrefab();

        var spawnA = new GameObject("SpawnPoint_1").transform;
        spawnA.position = new Vector3(-3f, 0f, 0f);
        var spawnB = new GameObject("SpawnPoint_2").transform;
        spawnB.position = new Vector3(3f, 0f, 0f);

        var statusLabel = CreateStatusLabel(font);

        var managerObj = new GameObject("CombatNetworkManager");
        var manager = managerObj.AddComponent<CombatNetworkManager>();
        manager.spawnPoints = new[] { spawnA, spawnB };
        manager.statusLabel = statusLabel;

        _ = networkPlayerPrefab; // 프리팹은 Resources 경로("NetworkPlayer")로 문자열 참조되어 인스펙터 연결이 필요 없음

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Selection.activeGameObject = managerObj;
        Debug.Log("1:1 네트워크 테스트 세팅 완료. Ctrl+S로 씬을 저장하세요.");
    }

    static GameObject GetOrCreateNetworkPlayerPrefab()
    {
        var existing = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (existing != null) return existing;

        var template = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        template.name = "NetworkPlayer";

        var transformView = template.AddComponent<PhotonTransformView>();

        var view = template.AddComponent<PhotonView>();
        view.ObservedComponents = new List<Component> { transformView };
        view.Synchronization = ViewSynchronization.UnreliableOnChange;

        template.AddComponent<NetworkPlayerController>();

        var prefab = PrefabUtility.SaveAsPrefabAsset(template, PrefabPath);
        Object.DestroyImmediate(template);
        return prefab;
    }

    static TMP_Text CreateStatusLabel(TMP_FontAsset font)
    {
        var canvasObj = new GameObject("NetworkStatusCanvas", typeof(RectTransform));
        var canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);

        var labelObj = new GameObject("StatusLabel", typeof(RectTransform));
        labelObj.transform.SetParent(canvasObj.transform, false);

        var rect = labelObj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.sizeDelta = new Vector2(500f, 40f);
        rect.anchoredPosition = new Vector2(20f, -20f);

        var text = labelObj.AddComponent<TextMeshProUGUI>();
        text.font = font;
        text.fontSize = 18f;
        text.alignment = TextAlignmentOptions.TopLeft;
        text.color = Color.white;
        text.text = "Idle";

        return text;
    }
}
