using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

// 무기 사거리를 Muzzle에서 실제 발사 방향으로 항상 그려서 보여준다.
// Gizmos는 에디터 Scene 뷰에서만 보이고 빌드에는 안 나오므로, LineRenderer로 그린다.
// 발사체가 여러 개면(증강으로 늘어난 만큼) 부채꼴로 퍼지는 각 방향마다 선을 하나씩 그린다.
// 내 조준선만 보여야 하므로, 네트워크 상 원격 플레이어(IsMine이 아님)면 아예 그리지 않는다.
[RequireComponent(typeof(WeaponController))]
[RequireComponent(typeof(LineRenderer))]
public class WeaponRangeIndicator : MonoBehaviour
{
    public PlayerAimController aim;
    public PlayerCombatContext combatContext;
    public Material lineMaterial;
    public float lineWidth = 0.05f;
    public Color lineColor = new Color(1f, 0f, 0f, 0.6f);

    WeaponController weapon;
    PhotonView ownerPhotonView;
    readonly List<LineRenderer> lines = new List<LineRenderer>();

    void Awake()
    {
        weapon = GetComponent<WeaponController>();
        ownerPhotonView = GetComponentInParent<PhotonView>();

        var baseLine = GetComponent<LineRenderer>();
        if (ownerPhotonView != null && !ownerPhotonView.IsMine)
            baseLine.enabled = false;
        lines.Add(baseLine); // 기본으로 붙어있는 LineRenderer를 0번 선으로 사용
    }

    void Update()
    {
        if (ownerPhotonView != null && !ownerPhotonView.IsMine) return;
        if (aim == null || combatContext == null || weapon.muzzle == null) return;

        Vector3 start = weapon.muzzle.position;

        // PlayerWeaponFireController.Fire()와 동일한 기준(Muzzle -> 마우스 월드 좌표)으로
        // 계산해야 실제 발사 방향과 표시선이 일치한다.
        Vector2 direction = (Vector2)aim.MouseWorldPosition - (Vector2)start;
        direction = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.right;

        int count = combatContext.EffectiveProjectileCount;
        EnsureLineCount(count);

        for (int i = 0; i < count; i++)
        {
            Vector2 fireDir = FanSpread.GetDirection(direction, i, weapon.Stats.spreadAngleDegrees);
            Vector3 end = start + (Vector3)(fireDir * weapon.Stats.projectileRange);

            lines[i].SetPosition(0, start);
            lines[i].SetPosition(1, end);
        }
    }

    void EnsureLineCount(int count)
    {
        while (lines.Count < count)
            lines.Add(CreateLine(lines.Count));

        for (int i = 0; i < lines.Count; i++)
            lines[i].enabled = i < count;
    }

    LineRenderer CreateLine(int index)
    {
        var obj = new GameObject($"RangeLine_{index}");
        obj.transform.SetParent(transform, false);

        var line = obj.AddComponent<LineRenderer>();
        line.positionCount = 2;
        line.useWorldSpace = true;
        line.startWidth = lineWidth;
        line.endWidth = lineWidth;
        line.numCapVertices = 4;
        line.material = lineMaterial;
        line.startColor = lineColor;
        line.endColor = lineColor;

        return line;
    }
}
