using UnityEngine;

// 게임 씬에서 주기적으로 재생되는 환경음(AMB) 하나를 나타내는 데이터. id는 AmbDatabase
// 조회 키, 70000번대(카테고리별 구간: Projectile 10000~/Weapon 20000~/ThrowableWeapon 30000~/
// Augment 40000~/Bgm 50000~/Sfx 60000~/Amb 70000~).
// 화면 전체에 균일하게 들려야 해서 SfxData와 달리 거리감쇠 관련 필드가 없다.
[CreateAssetMenu(fileName = "AmbData", menuName = "Sound/Amb Data")]
public class AmbData : ScriptableObject, IHasId
{
    [Header("ID (70001~)")]
    public int id;
    public int Id => id;

    public AudioClip clip;
    [Range(0f, 1f)] public float volume = 1f;

    [Header("재생 주기(초) - 한 번 재생 후 다음 재생까지 대기 시간")]
    public float interval = 15f;
}
