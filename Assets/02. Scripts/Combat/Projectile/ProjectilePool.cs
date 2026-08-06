using System.Collections.Generic;
using UnityEngine;

// Projectile 프리팹은 게임 로드/증강 선택 시점에 확정되고 발사마다 바뀌지 않으므로,
// 매번 Instantiate/Destroy하지 않고 미리 만들어둔 걸 꺼내 쓰고 반납받아 재사용한다.
public static class ProjectilePool
{
    const string ProjectilePrefabName = "Projectile";
    const int PrewarmCount = 50;

    static GameObject prefab;
    static Transform poolRoot;
    static readonly Stack<GameObject> pool = new Stack<GameObject>();

    public static GameObject Get(Vector3 position, Quaternion rotation)
    {
        EnsureInitialized();

        var obj = pool.Count > 0 ? pool.Pop() : Object.Instantiate(prefab);
        obj.transform.SetPositionAndRotation(position, rotation);
        obj.SetActive(true);
        return obj;
    }

    public static void Release(GameObject obj)
    {
        obj.SetActive(false);
        obj.transform.SetParent(poolRoot, false);
        pool.Push(obj);
    }

    static void EnsureInitialized()
    {
        if (prefab != null) return;

        prefab = Resources.Load<GameObject>(ProjectilePrefabName);
        poolRoot = new GameObject("ProjectilePool").transform;
        Object.DontDestroyOnLoad(poolRoot.gameObject);

        for (int i = 0; i < PrewarmCount; i++)
        {
            var obj = Object.Instantiate(prefab, poolRoot);
            obj.SetActive(false);
            pool.Push(obj);
        }
    }
}
