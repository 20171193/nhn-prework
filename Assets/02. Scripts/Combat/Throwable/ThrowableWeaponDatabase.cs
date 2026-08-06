using UnityEngine;

[CreateAssetMenu(fileName = "ThrowableWeaponDatabase", menuName = "Combat/Database/Throwable Weapon Database")]
public class ThrowableWeaponDatabase : Database<ThrowableWeaponData>
{
    // Assets/07. Data/Combat/Resources 기준 경로.
    const string ResourcesPath = "ThrowableWeaponDatabase";

    public static ThrowableWeaponDatabase Instance { get; private set; }

    public static void EnsureLoaded()
    {
        if (Instance == null)
            Instance = LoadFromResources<ThrowableWeaponDatabase>(ResourcesPath);
    }
}
