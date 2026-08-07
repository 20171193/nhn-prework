using UnityEngine;

// 효과음 하나를 나타내는 데이터. id는 SfxDatabase 조회 키, 60000번대.
[CreateAssetMenu(fileName = "SfxData", menuName = "Sound/Sfx Data")]
public class SfxData : ScriptableObject, IHasId
{
    [Header("ID (60001~)")]
    public int id;
    public int Id => id;

    public AudioClip clip;
    [Range(0f, 1f)] public float volume = 1f;

    // 2D 게임이지만 발사/피격 등은 화면이 넓어진 만큼 근원지에서 멀수록 작게 들려야 한다.
    // AudioSource를 spatialBlend=1(3D)로 재생해서 카메라의 AudioListener 기준 거리 감쇠를
    // 엔진이 알아서 계산하게 한다 - min 이내는 최대 볼륨, max 밖은 0.
    [Header("거리 감쇠")]
    public float minDistance = 3f;
    public float maxDistance = 20f;
}
