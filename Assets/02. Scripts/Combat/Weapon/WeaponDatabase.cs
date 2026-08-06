using UnityEngine;

[CreateAssetMenu(fileName = "WeaponDatabase", menuName = "Combat/Database/Weapon Database")]
public class WeaponDatabase : Database<WeaponData>
{
    // Assets/07. Data/Combat/Resources 기준 경로.
    const string ResourcesPath = "WeaponDatabase";

    public static WeaponDatabase Instance { get; private set; }

    public static void EnsureLoaded()
    {
        if (Instance == null)
            Instance = LoadFromResources<WeaponDatabase>(ResourcesPath);
    }
}
