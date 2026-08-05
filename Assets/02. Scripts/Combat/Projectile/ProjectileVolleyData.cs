using UnityEngine;

// 발사 RPC로 보낼 데이터 패킹. object[]/RPC 인자에 값 타입을 낱개로 넣으면 매번 박싱되지만,
// float[] 배열 하나에 담으면 배열 안의 값은 박싱되지 않는다(배열은 원래 참조 타입).
public static class ProjectileVolleyData
{
    public static float[] Pack(float speed, float damage, float range, Vector2[] directions)
    {
        var payload = new float[4 + directions.Length * 2];
        payload[0] = speed;
        payload[1] = damage;
        payload[2] = range;
        payload[3] = directions.Length;

        for (int i = 0; i < directions.Length; i++)
        {
            payload[4 + i * 2] = directions[i].x;
            payload[4 + i * 2 + 1] = directions[i].y;
        }

        return payload;
    }

    public static void Unpack(float[] payload, out float speed, out float damage, out float range, out Vector2[] directions)
    {
        speed = payload[0];
        damage = payload[1];
        range = payload[2];

        directions = new Vector2[(int)payload[3]];
        for (int i = 0; i < directions.Length; i++)
            directions[i] = new Vector2(payload[4 + i * 2], payload[4 + i * 2 + 1]);
    }
}
