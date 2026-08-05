using Photon.Pun;
using UnityEngine;

// 직선(Straight) 궤적 발사체. 사거리(range)만큼 이동하면 스스로 사라진다.
// 피격 판정은 맞은 대상 본인의 클라이언트가 내린다(지연 없는 자기 위치 기준이 정확함).
// 네트워크로는 발사 하나(발사체 여러 개 포함)당 한 번만 생성되고, 이 인스턴스가 0번을
// 맡고 나머지는 로컬로 복제한다 - 개별 발사체는 위치 동기화도 소유권도 필요 없다.
[RequireComponent(typeof(PhotonView))]
public class Projectile : MonoBehaviourPun, IPunInstantiateMagicCallback
{
    const string ProjectilePrefabName = "Projectile";

    Vector2 direction;
    float speed;
    float damage;
    float maxRange;
    Vector3 spawnPosition;

    public void OnPhotonInstantiate(PhotonMessageInfo info)
    {
        object[] data = photonView.InstantiationData;
        float spd = (float)data[0];
        float dmg = (float)data[1];
        float range = (float)data[2];
        int ownerViewId = (int)data[3];
        int count = (int)data[4];

        var ownerView = PhotonView.Find(ownerViewId);
        var ownerCollider = ownerView != null ? ownerView.GetComponent<Collider2D>() : null;
        var prefab = Resources.Load<GameObject>(ProjectilePrefabName);

        for (int i = 0; i < count; i++)
        {
            var dir = new Vector2((float)data[5 + i * 2], (float)data[5 + i * 2 + 1]);

            if (i == 0)
            {
                Init(dir, spd, dmg, range, ownerCollider);
            }
            else
            {
                var clone = Instantiate(prefab, transform.position, Quaternion.identity);
                clone.GetComponent<Projectile>().Init(dir, spd, dmg, range, ownerCollider);
            }
        }
    }

    void Init(Vector2 dir, float speed, float damage, float range, Collider2D ownerCollider)
    {
        direction = dir.normalized;
        this.speed = speed;
        this.damage = damage;
        maxRange = range;
        spawnPosition = transform.position;

        // 자신을 쏜 플레이어는 맞지 않도록 무시. 발사체끼리/벽은 레이어 매트릭스에서 처리.
        var myCollider = GetComponent<Collider2D>();
        if (ownerCollider != null && myCollider != null)
            Physics2D.IgnoreCollision(myCollider, ownerCollider);

        // 스프라이트가 기본적으로 오른쪽(+X)을 바라본다고 가정
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    void Update()
    {
        transform.position += (Vector3)(direction * speed * Time.deltaTime);

        if (Vector3.Distance(spawnPosition, transform.position) >= maxRange)
            Destroy(gameObject);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // 맞은 대상이 이 클라이언트 소유(본인)일 때만 판정한다.
        var targetView = other.GetComponent<PhotonView>();
        if (targetView != null && targetView.IsMine)
        {
            var damageable = other.GetComponent<IDamageable>();
            damageable?.ApplyDamage(damage);
        }

        Destroy(gameObject);
    }
}
