using UnityEngine;

// 투척 궤적(포물선 느낌)을 계산하는 공용 수식. 실제 중력/물리 계산 없이, start-end를
// 선형보간하면서 중간에서 최대가 되는 완만한 오프셋을 위로 더하는 근사치다.
// ThrowRangeIndicator(미리보기 곡선)와 ThrowableObject(실제 이동)가 같은 공식을 써야
// 미리보기와 실제 궤적이 어긋나지 않는다.
public static class ThrowArcMath
{
    // t: 0(start)~1(end). ratio는 거리/maxRange 비율로 minRatio~maxRatio 사이를 보간한다
    // (가까이 던지면 완만하게, 멀리 던지면 더 높게).
    public static Vector3 Evaluate(Vector3 start, Vector3 end, float t, float minRatio, float maxRatio, float maxRange)
    {
        float distance = Vector3.Distance(start, end);
        float ratio = maxRange > 0f
            ? Mathf.Lerp(minRatio, maxRatio, Mathf.Clamp01(distance / maxRange))
            : minRatio;
        float height = distance * ratio;

        return Vector3.Lerp(start, end, t) + Vector3.up * (height * 4f * t * (1f - t));
    }
}
