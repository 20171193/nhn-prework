using UnityEngine;

// 발사체가 여러 개일 때 조준 방향을 중심으로 부채꼴로 퍼뜨리는 계산.
// 실제 발사(PlayerWeaponFireController)와 사거리 표시선(WeaponRangeIndicator)이
// 항상 같은 방향을 가리켜야 하므로 계산을 한 곳에 모아둔다.
//
// 발사체 개수가 증강으로 늘어나도 index 0은 항상 마우스 에임 방향 그대로 나가야 하므로
// (좌우 대칭 분배가 아니라) 에임 방향을 중심으로 위/아래 번갈아 바깥으로 붙여나간다.
// index: 0 -> 에임, 1 -> +1칸(위), 2 -> -1칸(아래), 3 -> +2칸(위), 4 -> -2칸(아래) ...
public static class FanSpread
{
    public static Vector2 GetDirection(Vector2 aimDirection, int index, float spreadAngleDegrees)
    {
        float baseAngle = Mathf.Atan2(aimDirection.y, aimDirection.x) * Mathf.Rad2Deg;

        int step = (index + 1) / 2;
        float sign = index % 2 == 1 ? 1f : -1f; // 홀수(1,3,5..)=위, 짝수(2,4,6..)=아래, 0은 step 0이라 부호 무관
        float offset = sign * step * spreadAngleDegrees;

        float angle = (baseAngle + offset) * Mathf.Deg2Rad;
        return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
    }
}
