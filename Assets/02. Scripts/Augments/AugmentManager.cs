using System;
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
    readonly Func<PlayerCombatContext> localPlayerProvider;
    readonly List<AugmentData> owned = new List<AugmentData>();
    List<AugmentData> offers = new List<AugmentData>();

    // localPlayerProvider는 "지금 이 증강을 받을 플레이어"를 뽑기 시점에 알려주는 창구.
    // 플레이어를 직접 들고 있지 않고 매번 물어보는 이유는, 플레이어가 씬과 함께 사라졌다
    // 다시 생겨도(AugmentManager는 매치 내내 살아있다) 항상 최신 것을 보게 하기 위해서다.
    // 넘기지 않으면 조건부 증강도 전부 후보가 된다 - 플레이어 없이 도는 흐름 테스트용.
    public AugmentManager(AugmentCatalog catalog, Func<PlayerCombatContext> localPlayerProvider = null)
    {
        pool = new AugmentPool(catalog);
        this.localPlayerProvider = localPlayerProvider;
    }

    public IReadOnlyList<AugmentData> Owned => owned;

    public IReadOnlyList<AugmentData> Offers => offers;

    public List<AugmentData> RollAugments(int count = 3)
    {
        offers = pool.Draw(count, LocalPlayer);
        return offers;
    }

    // 슬롯 하나만 다시 뽑는다. 나머지 선택지와 겹치지 않게 뽑고, 바꾼 증강을 돌려준다.
    // 풀에 새로 보여줄 게 없으면 null을 주고 기존 선택지를 그대로 둔다.
    public AugmentData RerollAt(int index)
    {
        if (index < 0 || index >= offers.Count) return null;

        var replacement = pool.DrawOne(offers, LocalPlayer);
        if (replacement == null) return null;

        offers[index] = replacement;
        return replacement;
    }

    public void ChooseAugment(AugmentData augment)
    {
        if (augment == null) return;
        
        owned.Add(augment);
        pool.Remove(augment);
        offers.Clear();
    }

    PlayerCombatContext LocalPlayer => localPlayerProvider?.Invoke();
}
