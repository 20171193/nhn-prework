using System.Collections.Generic;
using UnityEngine;

// 한 플레이어의 증강 상태. 플레이어당 하나씩 만든다.
// DisplayAugments로 선택지를 뽑고, ChooseAugment로 획득하고,
// 라운드가 시작될 때 ApplyAll로 지금까지 쌓인 증강을 새 플레이어에 전부 먹인다.
// 획득한 증강은 라운드를 져도 지우지 않고 계속 쌓인다 (projectinfo.md 3-3).
// 초기화 코드가 아예 없어서 규칙이 자동으로 지켜진다.
public class AugmentManager
{
    readonly AugmentPool pool;
    readonly List<AugmentData> owned = new List<AugmentData>();
    List<AugmentData> offers = new List<AugmentData>();

    public AugmentManager(AugmentCatalog catalog)
    {
        pool = new AugmentPool(catalog);
    }

    public IReadOnlyList<AugmentData> Owned => owned;

    public IReadOnlyList<AugmentData> Offers => offers;

    public List<AugmentData> RollAugments(int count = 3)
    {
        offers = pool.Draw(count);
        return offers;
    }

    public void ChooseAugment(AugmentData augment)
    {
        if (augment == null) return;
        
        owned.Add(augment);
        pool.Remove(augment);
        offers.Clear();
    }
}
