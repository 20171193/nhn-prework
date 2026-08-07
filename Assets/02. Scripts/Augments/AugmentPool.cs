using System.Collections.Generic;

// 아직 뽑히지 않은 증강 목록. 플레이어당 하나씩 가진다.
// Draw는 선택지를 보여주기만 하고, 실제로 고른 증강만 Remove로 풀에서 빠진다.
// 티어가 낮을수록 자주 나온다.
//
// 뽑기 전에 지금 이 플레이어에게 의미가 있는 것만 남긴다(AugmentData.IsOfferable).
// 풀에서 빼지 않고 매번 거르는 이유는, 조건이 라운드마다 달라질 수 있기 때문이다 -
// 슬롯이 차서 지금은 못 주는 투척무기 증강도 상태가 바뀌면 다시 후보가 되어야 한다.
public class AugmentPool
{
    readonly List<AugmentData> available;

    public AugmentPool(AugmentCatalog catalog)
    {
        available = new List<AugmentData>(catalog.Augments);
    }

    public int Count => available.Count;

    public List<AugmentData> Draw(int count, PlayerCombatContext context)
    {
        var picked = new List<AugmentData>(count);
        var working = Offerable(context);

        while (picked.Count < count && working.Count > 0)
        {
            int index = WeightedIndex(working);
            picked.Add(working[index]);
            working.RemoveAt(index);
        }

        return picked;
    }

    // 리롤용. 지금 화면에 떠 있는 선택지를 빼고 하나만 더 뽑는다.
    // 뽑아도 풀에서 빼지 않는 것은 Draw와 같다. 실제로 고른 것만 Remove로 빠진다.
    // 보여줄 게 더 없으면 null을 준다 (풀이 거의 바닥난 마지막 라운드).
    public AugmentData DrawOne(List<AugmentData> exclude, PlayerCombatContext context)
    {
        var working = Offerable(context);
        if (exclude != null) working.RemoveAll(exclude.Contains);

        if (working.Count == 0) return null;
        return working[WeightedIndex(working)];
    }

    public void Remove(AugmentData augment)
    {
        available.Remove(augment);
    }

    // 남은 증강 중 지금 이 플레이어에게 내놓을 수 있는 것만 추린다.
    // 이미 갖고 있는 투척무기를 주는 증강 등이 여기서 빠진다.
    List<AugmentData> Offerable(PlayerCombatContext context)
    {
        var result = new List<AugmentData>(available.Count);
        foreach (var augment in available)
        {
            if (augment == null || !augment.IsOfferable(context)) continue;
            result.Add(augment);
        }

        return result;
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
