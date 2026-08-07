using UnityEngine;

[CreateAssetMenu(fileName = "SfxDatabase", menuName = "Sound/Database/Sfx Database")]
public class SfxDatabase : Database<SfxData>
{
    // Assets/07. Data/Sound/Resources 기준 경로.
    const string ResourcesPath = "SfxDatabase";

    public static SfxDatabase Instance { get; private set; }

    public static void EnsureLoaded()
    {
        if (Instance == null)
            Instance = LoadFromResources<SfxDatabase>(ResourcesPath);
    }
}
