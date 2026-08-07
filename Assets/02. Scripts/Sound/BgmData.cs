using UnityEngine;

// BGM 하나(오디오 클립)를 나타내는 데이터. id는 BgmDatabase에서 조회할 때 쓰는 키로,
// 50000번대를 쓴다(카테고리별 구간: Projectile 10000~/Weapon 20000~/ThrowableWeapon 30000~/
// Augment 40000~/Bgm 50000~/Sfx 60000~).
[CreateAssetMenu(fileName = "BgmData", menuName = "Sound/Bgm Data")]
public class BgmData : ScriptableObject, IHasId
{
    [Header("ID (50001~)")]
    public int id;
    public int Id => id;

    public AudioClip clip;
    [Range(0f, 1f)] public float volume = 1f;
    public bool loop = true;
}
