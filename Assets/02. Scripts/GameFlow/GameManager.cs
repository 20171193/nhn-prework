using System;
using System.Collections;
using System.Collections.Generic;
using Core;
using UnityEngine;

public class GameManager : Singleton<GameManager>
{
    private AugmentManager augmentManager;
    [SerializeField] private AugmentCatalog augmentCatalog;
    [SerializeField] private AugmentSelectionUI augmentSelectionUI;

    private bool isPlayerChoosingAugment;

    protected override void Awake()
    {
        augmentManager = new AugmentManager(augmentCatalog);
    }
    
    [ContextMenu("Offer Augments")]
    public void OfferAugments()
    {
        StartCoroutine(GameFlow());
    }
    
    private IEnumerator GameFlow()
    {
        // pause
        
        yield return YieldCache.WaitForSeconds(1);
        
        isPlayerChoosingAugment = true;
        augmentSelectionUI.Show(augmentManager, OnAugmentChosen);

        yield return new WaitUntil(() => isPlayerChoosingAugment == false);
        
        // continue
    }

    private void OnAugmentChosen(AugmentData data)
    {
        isPlayerChoosingAugment = false;
    }
}
