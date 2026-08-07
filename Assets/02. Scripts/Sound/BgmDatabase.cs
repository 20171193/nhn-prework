using UnityEngine;

[CreateAssetMenu(fileName = "BgmDatabase", menuName = "Sound/Database/Bgm Database")]
public class BgmDatabase : Database<BgmData>
{
    // Assets/07. Data/Sound/Resources 기준 경로.
    const string ResourcesPath = "BgmDatabase";

    public static BgmDatabase Instance { get; private set; }

    public static void EnsureLoaded()
    {
        if (Instance == null)
            Instance = LoadFromResources<BgmDatabase>(ResourcesPath);
    }
}
