using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Tools > Combat Prototype > Build Control Prototype Scene 실행 시
// 조작 방식 테스트용 오브젝트(Player/DummyTarget/Projectile 프리팹)를
// 현재 열려있는 씬에 배치하고, PlayerController의 참조를 미리 캐싱해준다.
public static class ControlPrototypeSceneSetup
{
    const string ProjectilePrefabPath = "Assets/02. Scripts/Prototype/Projectile.prefab";

    [MenuItem("Tools/Combat Prototype/Build Control Prototype Scene")]
    static void Build()
    {
        if (GameObject.Find("Player") != null)
        {
            Debug.LogWarning("씬에 Player가 이미 있습니다. 다시 만들려면 기존 Player/DummyTarget을 지운 뒤 다시 실행하세요.");
            return;
        }

        GameObject projectilePrefab = GetOrCreateProjectilePrefab();

        var player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        player.name = "Player";
        player.transform.position = Vector3.zero;
        player.GetComponent<Renderer>().material.color = Color.blue;

        var nose = GameObject.CreatePrimitive(PrimitiveType.Cube);
        nose.name = "Nose";
        Object.DestroyImmediate(nose.GetComponent<Collider>());
        nose.transform.SetParent(player.transform);
        nose.transform.localPosition = new Vector3(0f, 0.6f, 0f);
        nose.transform.localScale = new Vector3(0.25f, 0.25f, 0.25f);

        var target1 = SpawnTarget(new Vector3(5f, 3f, 0f));
        var target2 = SpawnTarget(new Vector3(-4f, 5f, 0f));
        var target3 = SpawnTarget(new Vector3(3f, -4f, 0f));

        var controller = player.AddComponent<PlayerController>();
        controller.projectilePrefab = projectilePrefab;
        controller.dummyTargets = new[] { target1.transform, target2.transform, target3.transform };

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Selection.activeGameObject = player;
        Debug.Log("Control Prototype 씬 구성 완료. Ctrl+S로 씬을 저장하세요.");
    }

    static GameObject GetOrCreateProjectilePrefab()
    {
        var existing = AssetDatabase.LoadAssetAtPath<GameObject>(ProjectilePrefabPath);
        if (existing != null) return existing;

        var template = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        template.name = "Projectile";
        template.transform.localScale = Vector3.one * 0.3f;
        template.GetComponent<Renderer>().material.color = Color.yellow;

        var col = template.GetComponent<SphereCollider>();
        col.isTrigger = true;

        var rb = template.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;

        template.AddComponent<Projectile>();

        var prefab = PrefabUtility.SaveAsPrefabAsset(template, ProjectilePrefabPath);
        Object.DestroyImmediate(template);
        return prefab;
    }

    static GameObject SpawnTarget(Vector3 position)
    {
        var target = GameObject.CreatePrimitive(PrimitiveType.Cube);
        target.name = "DummyTarget";
        target.transform.position = position;
        target.GetComponent<Renderer>().material.color = Color.gray;
        target.AddComponent<DummyTarget>();
        return target;
    }
}
