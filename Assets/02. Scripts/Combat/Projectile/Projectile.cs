using Photon.Pun;
using UnityEngine;

// 직선(Straight) 궤적 발사체. 사거리(range)만큼 이동하면 스스로 사라진다.
// 피격 판정(데미지 적용)은 상대 스탯 시스템이 준비되면 여기에 이어서 붙인다.
// PhotonNetwork.Instantiate로 생성되며, 발사 시점 값은 PhotonWeaponFireController가
// instantiationData로 넘긴 것을 OnPhotonInstantiate에서 모든 클라이언트가 동일하게 읽어
// 각자 로컬로 시뮬레이션한다(위치 자체를 매 프레임 동기화하지 않음).
[RequireComponent(typeof(PhotonView))]
public class Projectile : MonoBehaviourPun, IPunInstantiateMagicCallback
{
    Vector2 direction;
    float speed;
    float damage;
    float maxRange;
    Vector3 spawnPosition;

    public void OnPhotonInstantiate(PhotonMessageInfo info)
    {
        object[] data = photonView.InstantiationData;
        var dir = new Vector2((float)data[0], (float)data[1]);
        float spd = (float)data[2];
        float dmg = (float)data[3];
        float range = (float)data[4];
        int ownerViewId = (int)data[5];

        Init(dir, spd, dmg, range);
        IgnoreOwnerCollision(ownerViewId);
    }

    void Init(Vector2 dir, float speed, float damage, float range)
    {
        direction = dir.normalized;
        this.speed = speed;
        this.damage = damage;
        maxRange = range;
        spawnPosition = transform.position;

        // 스프라이트가 기본적으로 오른쪽(+X)을 바라본다고 가정
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    // 자신을 쏜 플레이어는 맞지 않도록 무시. 클라이언트마다 로컬로 물리를 시뮬레이션하므로
    // 각 클라이언트에서 자기 자신이 들고 있는 owner 콜라이더를 찾아 개별적으로 무시 처리한다.
    // 발사체끼리/벽은 레이어 충돌 매트릭스에서 처리한다.
    void IgnoreOwnerCollision(int ownerViewId)
    {
        var ownerView = PhotonView.Find(ownerViewId);
        if (ownerView == null) return;

        var ownerCollider = ownerView.GetComponent<Collider2D>();
        var myCollider = GetComponent<Collider2D>();
        if (ownerCollider != null && myCollider != null)
            Physics2D.IgnoreCollision(myCollider, ownerCollider);
    }

    void Update()
    {
        transform.position += (Vector3)(direction * speed * Time.deltaTime);

        if (Vector3.Distance(spawnPosition, transform.position) >= maxRange)
            DestroyIfMine();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        DestroyIfMine();
    }

    // 네트워크 오브젝트는 소유자만 파괴를 요청할 수 있다.
    void DestroyIfMine()
    {
        if (photonView.IsMine)
            PhotonNetwork.Destroy(gameObject);
    }
}
