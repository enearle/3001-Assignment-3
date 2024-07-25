using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour, IDamageable
{
    Rigidbody2D rb;
    const float moveForce = 25.0f;
    const float maxSpeed = 10.0f;
    private const float coolDownDuration = 0.25f;
    private float coolDownTimer = 0;
    private bool onCoolDown = false;
    [SerializeField]
    GameObject bulletPrefab;
    public int health { get; private set; } = 100;
    public bool isPlayer { get; private set; } = true;
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();    
    }

    void Update()
    {
        Vector2 direction = Vector2.zero;
        if (Input.GetKey(KeyCode.W))
        {
            direction += Vector2.up;
        }
        else if (Input.GetKey(KeyCode.S))
        {
            direction += Vector2.down;
        }
        if (Input.GetKey(KeyCode.A))
        {
            direction += Vector2.left;
        }
        else if (Input.GetKey(KeyCode.D))
        {
            direction += Vector2.right;
        }
        direction = direction.normalized;
        rb.AddForce(direction * moveForce);

        if (onCoolDown)
        {
            coolDownTimer += Time.deltaTime;
            if (coolDownTimer >= coolDownDuration)
            {
                coolDownTimer = 0;
                onCoolDown = false;
            }
        }
        
        if (Input.GetMouseButtonDown(0) && !onCoolDown)
        {
            Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition) - Camera.main.transform.position;
            Vector3 mouseDir = (mousePos - transform.position).normalized;
            GameObject bullet = Instantiate(bulletPrefab);
            bullet.GetComponent<Bullet>().playerOwned = isPlayer;
            bullet.transform.position = transform.position; // + mouseDir * 2; // removing offset to prove bullet collision case
            bullet.GetComponent<Rigidbody2D>().velocity = mouseDir * 10.0f;
            Destroy(bullet, 1.0f);
            onCoolDown = true;
        }
        
        // Limit velocity
        if (rb.velocity.magnitude > maxSpeed)
            rb.velocity = rb.velocity.normalized * maxSpeed;
    }
    
    public void Damage(int damage)
    {
        health -= damage;
        if (health <= 0)
        {
            Destroy(gameObject);
        }
    }
}
