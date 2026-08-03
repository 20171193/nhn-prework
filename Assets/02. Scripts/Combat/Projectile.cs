using UnityEngine;

// 직선(Straight) 궤적 발사체. 사거리(range)만큼 이동하면 스스로 사라진다.
// 피격 판정(데미지 적용)은 상대 스탯 시스템이 준비되면 여기에 이어서 붙인다.
public class Projectile : MonoBehaviour
{
    Vector2 direction;
    float speed;
    float damage;
    float maxRange;
    Vector3 spawnPosition;

    public void Init(Vector2 dir, ProjectileStats stats, float range)
    {
        direction = dir.normalized;
        speed = stats.speed;
        damage = stats.damage;
        maxRange = range;
        spawnPosition = transform.position;

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
        Destroy(gameObject);
    }
}
