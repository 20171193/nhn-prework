using System.Collections.Generic;

// 아직 뽑히지 않은 증강 목록. 플레이어당 하나씩 가진다.
// Draw는 선택지를 보여주기만 하고, 실제로 고른 증강만 Remove로 풀에서 빠진다.
// 티어가 낮을수록 자주 나온다.
public class AugmentPool
{
    readonly List<AugmentData> available;

    public AugmentPool(AugmentCatalog catalog)
    {
        available = new List<AugmentData>(catalog.Augments);
    }

    public int Count => available.Count;

    public List<AugmentData> Draw(int count = 3)
    {
        var picked = new List<AugmentData>(count);
        var working = new List<AugmentData>(available);

        while (picked.Count < count && working.Count > 0)
        {
            int index = WeightedIndex(working);
            picked.Add(working[index]);
            working.RemoveAt(index);
        }

        return picked;
    }

    public void Remove(AugmentData augment)
    {
        available.Remove(augment);
    }

    static int WeightedIndex(List<AugmentData> pool)
    {
        float total = 0f;
        for (int i = 0; i < pool.Count; i++) total += WeightOf(pool[i].tier);

        float roll = UnityEngine.Random.value * total;
        for (int i = 0; i < pool.Count; i++)
        {
            roll -= WeightOf(pool[i].tier);
            if (roll <= 0f) return i;
        }
        return pool.Count - 1;
    }

    // 카탈로그 인스펙터가 티어별 뽑기 비중을 표시할 때도 이 값을 쓴다.
    // 밸런스를 여기서 바꾸면 인스펙터 표시도 같이 따라간다.
    public static float WeightOf(AugmentTier tier)
    {
        switch (tier)
        {
            case AugmentTier.Rare:      return 8f;
            case AugmentTier.Epic:      return 4f;
            case AugmentTier.Unique:    return 2f;
            case AugmentTier.Legendary: return 1f;
            default:                    return 1f;
        }
    }
}
