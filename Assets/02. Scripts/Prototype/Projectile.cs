using UnityEngine;

public class Projectile : MonoBehaviour
{
    public float lifeTime = 3f;

    Vector2 direction;
    float speed;

    public void Init(Vector2 dir, float spd)
    {
        direction = dir;
        speed = spd;
        CancelInvoke();
        Invoke(nameof(Expire), lifeTime);
    }

    void Update()
    {
        transform.position += (Vector3)(direction * speed * Time.deltaTime);
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out DummyTarget target))
        {
            target.Hit();
            Expire();
        }
    }

    void Expire()
    {
        gameObject.SetActive(false);
        Destroy(gameObject);
    }
}
