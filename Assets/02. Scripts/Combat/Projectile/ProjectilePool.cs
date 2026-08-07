using System.Collections.Generic;
using UnityEngine;

// 발사체 프리팹을 매번 Instantiate/Destroy하지 않고 꺼내 쓰고 반납받아 재사용한다.
// 무기마다 발사체 프리팹이 다를 수 있어서(ProjectileData.prefab) 프리팹별로 풀을 따로 둔다.
// 처음 보는 프리팹은 그 자리에서 하나 Instantiate해서 채운다(지연 생성) - 첫 발사만
// 약간의 비용이 들고, 그 뒤로는 반납된 걸 재사용한다.
public static class ProjectilePool
{
    static Transform poolRoot;
    static readonly Dictionary<GameObject, Stack<GameObject>> pools = new Dictionary<GameObject, Stack<GameObject>>();
    // Release 시 어느 풀로 돌려보내야 할지 찾기 위한 역방향 매핑(인스턴스 -> 원본 프리팹).
    static readonly Dictionary<GameObject, GameObject> prefabOfInstance = new Dictionary<GameObject, GameObject>();

    public static GameObject Get(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        EnsureRoot();

        var pool = GetOrCreatePool(prefab);
        var obj = pool.Count > 0 ? pool.Pop() : Object.Instantiate(prefab);

        prefabOfInstance[obj] = prefab;
        obj.transform.SetPositionAndRotation(position, rotation);
        obj.SetActive(true);
        return obj;
    }

    public static void Release(GameObject obj)
    {
        obj.SetActive(false);
        obj.transform.SetParent(poolRoot, false);

        if (prefabOfInstance.TryGetValue(obj, out var prefab))
            GetOrCreatePool(prefab).Push(obj);
        else
            Object.Destroy(obj); // 어느 풀에서 나왔는지 알 수 없는 경우의 안전망
    }

    static Stack<GameObject> GetOrCreatePool(GameObject prefab)
    {
        if (!pools.TryGetValue(prefab, out var pool))
        {
            pool = new Stack<GameObject>();
            pools[prefab] = pool;
        }
        return pool;
    }

    static void EnsureRoot()
    {
        if (poolRoot != null) return;

        poolRoot = new GameObject("ProjectilePool").transform;
        Object.DontDestroyOnLoad(poolRoot.gameObject);
    }
}
