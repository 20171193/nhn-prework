using UnityEngine;

// 발사 RPC로 보낼 데이터 패킹. object[]/RPC 인자에 값 타입을 낱개로 넣으면 매번 박싱되지만,
// float[] 배열 하나에 담으면 배열 안의 값은 박싱되지 않는다(배열은 원래 참조 타입).
// projectileId는 어떤 ProjectileData(=어떤 프리팹)를 스폰할지 수신 측이 판단하는 데 쓴다.
public static class ProjectileVolleyData
{
    public static float[] Pack(int projectileId, float speed, float damage, float range, Vector2[] directions)
    {
        var payload = new float[5 + directions.Length * 2];
        payload[0] = projectileId;
        payload[1] = speed;
        payload[2] = damage;
        payload[3] = range;
        payload[4] = directions.Length;

        for (int i = 0; i < directions.Length; i++)
        {
            payload[5 + i * 2] = directions[i].x;
            payload[5 + i * 2 + 1] = directions[i].y;
        }

        return payload;
    }

    public static void Unpack(float[] payload, out int projectileId, out float speed, out float damage, out float range, out Vector2[] directions)
    {
        projectileId = (int)payload[0];
        speed = payload[1];
        damage = payload[2];
        range = payload[3];

        directions = new Vector2[(int)payload[4]];
        for (int i = 0; i < directions.Length; i++)
            directions[i] = new Vector2(payload[5 + i * 2], payload[5 + i * 2 + 1]);
    }
}
