using System.Collections.Generic;
using UnityEngine;

// 게임에 존재하는 증강 전체 목록. 여기에 등록된 것만 풀에 들어간다.
[CreateAssetMenu(fileName = "AugmentCatalog", menuName = "Augments/Catalog")]
public class AugmentCatalog : ScriptableObject
{
    public List<AugmentData> Augments = new List<AugmentData>();

    public int Count => Augments.Count;

    public bool TryGet(int id, out AugmentData data)
    {
        foreach (var augment in Augments)
        {
            if (augment != null && augment.Id == id)
            {
                data = augment;
                return true;
            }
        }

        data = null;
        return false;
    }
}
