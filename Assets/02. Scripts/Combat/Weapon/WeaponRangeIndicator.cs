using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

// 무기 사거리를 Muzzle에서 실제 발사 방향으로 항상 그려서 보여준다.
// Gizmos는 에디터 Scene 뷰에서만 보이고 빌드에는 안 나오므로, LineRenderer로 그린다.
// 0번(항상 존재하는 첫 발사체)은 인스펙터에 원래 붙어있는 baseLine 색 그대로 두고,
// 증강으로 늘어난 나머지 발사체는 발사체 1개당 LineRenderer 1개씩 따로 그린다.
// (예전에는 LineRenderer 하나로 왕복해서 여러 발사체를 표현했는데, 겹치는 구간의 알파가
// 누적되어 진해 보이는 문제가 있어 이 방식으로 바꿨다.)
//
// extraLineRoot 아래에 Line0, Line1... 을 에디터에서 미리 배치해두면 Awake에서 캐싱만 하고
// 런타임에 새로 만들지 않는다. 발사체 개수는 WeaponData.projectileCount가 [Range(1,5)]로
// 막혀 있어 최대 4개뿐이라 이 정도면 충분하지만, 혹시 그 이상이 필요해지는 예외적인 경우를
// 대비해 부족하면 그때만 방어적으로 추가 생성한다(정상 플레이에서는 발동하지 않는 경로).
//
// 내 조준선만 보여야 하므로, 네트워크 상 원격 플레이어(IsMine이 아님)면 아예 그리지 않는다.
[RequireComponent(typeof(WeaponController))]
public class WeaponRangeIndicator : MonoBehaviour
{
    [SerializeField] private WeaponController weapon;
    [SerializeField] private PhotonView ownerPhotonView;
    [SerializeField] private LineRenderer baseLine;      // 0번 발사체 전용, 인스펙터 설정 색 그대로
    [SerializeField] private Transform extraLineRoot;    // 1번 이후 발사체용 LineRenderer들의 부모(Line0, Line1...)

    readonly List<LineRenderer> extraLines = new List<LineRenderer>();

    PlayerAimController aim;
    PlayerCombatContext combatContext;
    ThrowableWeaponController throwableWeapon;
    PlayerWeaponFireController fireController;

    void Awake()
    {
        aim = GetComponentInParent<PlayerAimController>();
        combatContext = GetComponentInParent<PlayerCombatContext>();
        ownerPhotonView = GetComponentInParent<PhotonView>();
        throwableWeapon = GetComponentInParent<ThrowableWeaponController>();
        fireController = GetComponent<PlayerWeaponFireController>();

        if (extraLineRoot != null)
        {
            foreach (Transform child in extraLineRoot)
            {
                var line = child.GetComponent<LineRenderer>();
                if (line == null) continue;

                extraLines.Add(line);
                line.enabled = false;
            }
        }

        if (ownerPhotonView != null && !ownerPhotonView.IsMine)
        {
            baseLine.enabled = false;
            SetAllExtraLinesEnabled(false);
        }
    }

    void Update()
    {
        if (ownerPhotonView != null && !ownerPhotonView.IsMine) return;
        if (aim == null || combatContext == null || weapon.muzzle == null) return;

        // 투척 스킬 에임 중이거나(ThrowRangeIndicator가 대신 그려줌), 발사 딜레이 중(아직 못 쏨)이면 숨긴다.
        bool isThrowAiming = throwableWeapon != null && throwableWeapon.IsAiming;
        bool canFireNow = fireController == null || fireController.CanFire;
        bool showLines = !isThrowAiming && canFireNow;

        baseLine.enabled = showLines;
        if (!showLines)
        {
            SetAllExtraLinesEnabled(false);
            return;
        }

        Vector3 start = weapon.muzzle.position;

        // PlayerWeaponFireController.Fire()와 동일한 기준(Muzzle -> 마우스 월드 좌표)으로
        // 계산해야 실제 발사 방향과 표시선이 일치한다.
        Vector2 direction = (Vector2)aim.MouseWorldPosition - (Vector2)start;
        direction = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.right;

        int count = combatContext.EffectiveProjectileCount;
        float spread = weapon.Stats.spreadAngleDegrees;
        float range = weapon.Stats.projectileRange;

        Vector2 baseDir = FanSpread.GetDirection(direction, 0, spread);
        baseLine.positionCount = 2;
        baseLine.SetPosition(0, start);
        baseLine.SetPosition(1, start + (Vector3)(baseDir * range));

        int extraCount = Mathf.Max(0, count - 1);
        for (int i = 0; i < extraCount; i++)
        {
            Vector2 fireDir = FanSpread.GetDirection(direction, i + 1, spread);
            Vector3 end = start + (Vector3)(fireDir * range);

            var line = GetExtraLine(i);
            line.enabled = true;
            line.positionCount = 2;
            line.SetPosition(0, start);
            line.SetPosition(1, end);
        }

        // 이번 프레임에 안 쓴 나머지는 꺼둔다.
        for (int i = extraCount; i < extraLines.Count; i++)
            extraLines[i].enabled = false;
    }

    // 에디터에서 미리 배치해둔 만큼(extraLines)을 넘어서는 경우에만 방어적으로 추가 생성한다.
    // 정상 플레이(발사체 최대 5개, 즉 추가 4개)에서는 이 경로를 탈 일이 없다.
    LineRenderer GetExtraLine(int index)
    {
        if (index < extraLines.Count) return extraLines[index];

        var go = new GameObject($"Line{index}");
        go.transform.SetParent(extraLineRoot != null ? extraLineRoot : transform, false);

        var line = go.AddComponent<LineRenderer>();
        CopyLineStyle(extraLines.Count > 0 ? extraLines[0] : baseLine, line);

        extraLines.Add(line);
        return line;
    }

    // 미리 배치해둔 라인이 하나도 없을 때를 대비해, 기존 라인(있으면 extraLines[0], 없으면
    // baseLine)의 룩(머티리얼/두께/정렬/색상)을 그대로 복사해서 새 라인도 똑같이 보이게 한다.
    // 색상(알파 포함)도 여기서 원본 그대로 복사하고, 이후 코드는 색을 따로 덮어쓰지 않는다.
    void CopyLineStyle(LineRenderer from, LineRenderer to)
    {
        to.sharedMaterial = from.sharedMaterial;
        to.widthMultiplier = from.widthMultiplier;
        to.widthCurve = from.widthCurve;
        to.numCapVertices = from.numCapVertices;
        to.numCornerVertices = from.numCornerVertices;
        to.useWorldSpace = from.useWorldSpace;
        to.sortingLayerID = from.sortingLayerID;
        to.sortingOrder = from.sortingOrder;
        to.startColor = from.startColor;
        to.endColor = from.endColor;
    }

    void SetAllExtraLinesEnabled(bool enabled)
    {
        for (int i = 0; i < extraLines.Count; i++)
            extraLines[i].enabled = enabled;
    }
}
