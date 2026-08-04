using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Tools > Combat Prototype > Add Weapon Range Indicator 실행 시
// Weapon에 LineRenderer + WeaponRangeIndicator를 붙여 Muzzle부터 사거리까지
// 선을 항상 그리도록 세팅한다. 이미 되어 있으면 건너뛰므로 여러 번 실행해도 안전하다.
public static class WeaponRangeIndicatorSetup
{
    [MenuItem("Tools/Combat Prototype/Add Weapon Range Indicator")]
    static void Build()
    {
        var player = GameObject.Find("Player");
        if (player == null)
        {
            Debug.LogWarning("씬에서 Player를 찾을 수 없습니다.");
            return;
        }

        var weaponTransform = player.transform.Find("Weapon");
        if (weaponTransform == null)
        {
            Debug.LogWarning("Player 아래에서 Weapon을 찾을 수 없습니다. 먼저 'Add Core Combat Stats'를 실행하세요.");
            return;
        }

        var aim = player.GetComponent<PlayerAimController>();
        if (aim == null)
        {
            Debug.LogWarning("Player에 PlayerAimController가 없습니다. 먼저 'Add Core Combat Stats'를 실행하세요.");
            return;
        }

        var weaponObj = weaponTransform.gameObject;

        var line = weaponObj.GetComponent<LineRenderer>();
        if (line == null) line = weaponObj.AddComponent<LineRenderer>();
        line.positionCount = 2;
        line.useWorldSpace = true;
        line.startWidth = 0.05f;
        line.endWidth = 0.05f;
        line.numCapVertices = 4;
        line.material = AssetDatabase.GetBuiltinExtraResource<Material>("Default-Line.mat");
        line.startColor = new Color(1f, 0f, 0f, 0.6f);
        line.endColor = new Color(1f, 0f, 0f, 0.6f);

        var indicator = weaponObj.GetComponent<WeaponRangeIndicator>();
        if (indicator == null) indicator = weaponObj.AddComponent<WeaponRangeIndicator>();
        indicator.aim = aim;

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Selection.activeGameObject = weaponObj;
        Debug.Log("무기 사거리 표시선 세팅 완료. Ctrl+S로 씬을 저장하세요.");
    }
}
