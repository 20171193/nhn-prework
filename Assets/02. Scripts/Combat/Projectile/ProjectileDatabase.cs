using UnityEngine;

[CreateAssetMenu(fileName = "ProjectileDatabase", menuName = "Combat/Database/Projectile Database")]
public class ProjectileDatabase : Database<ProjectileData>
{
    // Assets/07. Data/Combat/Resources 기준 경로.
    const string ResourcesPath = "ProjectileDatabase";

    public static ProjectileDatabase Instance { get; private set; }

    public static void EnsureLoaded()
    {
        if (Instance == null)
            Instance = LoadFromResources<ProjectileDatabase>(ResourcesPath);
    }
}
