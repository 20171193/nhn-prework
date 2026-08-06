using System.Collections.Generic;
using UnityEngine;

// id로 조회 가능해야 하는 데이터(무기/발사체/스킬 등)의 공통 계약.
public interface IHasId
{
    int Id { get; }
}

// ID 기반 카탈로그의 공용 베이스. 카테고리별 Database(WeaponDatabase 등)는
// 이 클래스만 상속하면 TryGet을 그대로 쓸 수 있다.
public abstract class Database<T> : ScriptableObject where T : Object, IHasId
{
    public List<T> entries = new List<T>();

    public bool TryGet(int id, out T data)
    {
        foreach (var entry in entries)
        {
            if (entry != null && entry.Id == id)
            {
                data = entry;
                return true;
            }
        }

        data = null;
        return false;
    }

    // Resources 아래 있는 Database 에셋을 로드한다. 에셋 자체는 게임 시작 시 Bootstrap이
    // 한 번만 불러 static Instance에 캐싱해두므로(각 Database 하위 클래스의 EnsureLoaded 참고),
    // 인게임 런타임 중에는 이 메서드가 다시 불릴 일이 없다.
    protected static TDb LoadFromResources<TDb>(string resourcesPath) where TDb : Database<T>
    {
        var db = Resources.Load<TDb>(resourcesPath);
        if (db == null)
            Debug.LogError($"Resources/{resourcesPath} 에서 {typeof(TDb).Name}를 찾지 못했습니다.");

        return db;
    }
}
