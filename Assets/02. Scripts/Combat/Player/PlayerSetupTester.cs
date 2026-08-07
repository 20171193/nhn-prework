using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerSetupTester : MonoBehaviour
{
    [SerializeField] private string playerName;
    [SerializeField] private int profileIconId;
    [SerializeField] private int weaponId;
    [SerializeField] private int throwableWeaponId;

    void Awake()
    {
        PlayData.SetName(playerName);
        PlayData.SetProfileIcon(profileIconId);
        PlayData.SetWeapon(weaponId);
        PlayData.SetThrowableWeapon(throwableWeaponId);
    }
}
