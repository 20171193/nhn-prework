using UnityEngine;

[CreateAssetMenu(fileName = "AmbDatabase", menuName = "Sound/Database/Amb Database")]
public class AmbDatabase : Database<AmbData>
{
    // Assets/07. Data/Sound/Resources 기준 경로.
    const string ResourcesPath = "AmbDatabase";

    public static AmbDatabase Instance { get; private set; }

    public static void EnsureLoaded()
    {
        if (Instance == null)
            Instance = LoadFromResources<AmbDatabase>(ResourcesPath);
    }
}
