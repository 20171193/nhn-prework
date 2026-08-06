using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerSetupTester : MonoBehaviour
{
    [SerializeField] private string playerName;
    [SerializeField] private int profileIconId;
    [SerializeField] private int weaponId;

    void Awake()
    {
        LocalPlayerSetup.SetName(playerName);
        LocalPlayerSetup.SetProfileIcon(profileIconId);
        LocalPlayerSetup.SetWeapon(weaponId);
    }
}
