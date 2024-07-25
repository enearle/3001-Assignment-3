using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy : MonoBehaviour, IDamageable
{
    [SerializeField]
    Transform player;

    Rigidbody2D rb;

    public Transform[] waypoints;
    int waypoint = 0;
    private int lastWaypoint = 0;

    const float moveSpeed = 7.5f;
    const float turnSpeed = 1080.0f;
    const float viewDistance = 5.0f;


    private const int maxHealth = 100;
    public int health { get; private set; } = 100;
    public bool isPlayer { get; private set; } = false;
    
    [SerializeField]
    GameObject bulletPrefab;
    Timer shootCooldown = new Timer();
    
    Timer defenceTimer = new Timer();
    
    Color color = Color.cyan;

    enum State
    {
        DEFAULT,
        NEUTRAL,
        OFFENSIVE,
        DEFENSIVE
    };


    // If enemy drops below 25% health, flee and shoot!
    State state = State.DEFAULT;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        shootCooldown.total = 0.25f;
        defenceTimer.total = 20f;
        StateChange(State.NEUTRAL);
    }

    void Update()
    {
        float rotation = Steering.RotateTowardsVelocity(rb, turnSpeed, Time.deltaTime);
        rb.MoveRotation(rotation);

        if (player != null)
        {
            float playerDistance = Vector2.Distance(transform.position, player.position);

            if (state != State.DEFENSIVE)
            {
                StateChange(playerDistance <= viewDistance ? State.OFFENSIVE : State.NEUTRAL);
            }
        }
        else
        {
            StateChange(State.NEUTRAL);
        }

        // Repeating state-based actions:
        switch (state)
        {
            case State.NEUTRAL:
                Patrol();
                break;

            case State.OFFENSIVE:
                Attack();
                break;
            
            case State.DEFENSIVE:
                Defend();
                break;
        }
        
        Debug.DrawLine(transform.position, transform.position + transform.right * viewDistance, color);
    }

    public void Damage(int damage)
    {
        health -= damage;
        
        // Begin defensive behaviour
        if(health < maxHealth / 4)
            StateChange(State.DEFENSIVE);
        
        if (health <= 0)
        {
            Destroy(gameObject);
        }
    }
    
    private void Defend()
    {
        // Manage duration of defense or comment out for infinite duration
        defenceTimer.Tick(Time.deltaTime);
        if (defenceTimer.Expired())
        {
            StateChange(State.NEUTRAL);
            return;
        }
        
        // Seek the farthest waypoint from player
        Vector3 steeringForce = Vector2.zero;
        steeringForce += Steering.Seek(rb, waypoints[waypoint].transform.position, moveSpeed);
        rb.AddForce(steeringForce);
        
        // LOS to player
        Vector3 playerDirection = (player.position - transform.position).normalized;
        RaycastHit2D hit = Physics2D.Raycast(transform.position, playerDirection, viewDistance);
        bool playerHit = hit && hit.collider.CompareTag("Player");

        // Shoot player if in LOS at 1/5 the normal rate
        shootCooldown.Tick(Time.deltaTime / 5);
        if (playerHit && shootCooldown.Expired())
        {
            shootCooldown.Reset();
            GameObject bullet = Instantiate(bulletPrefab);
            bullet.GetComponent<Bullet>().playerOwned = isPlayer;
            bullet.transform.position = transform.position; // + playerDirection; // removing offset to prove bullet collision case
            bullet.GetComponent<Rigidbody2D>().velocity = playerDirection * 10.0f;
            Destroy(bullet, 1.0f);
        }
    }

    void Attack()
    {
        // Seek player
        Vector3 steeringForce = Vector2.zero;
        steeringForce += Steering.Seek(rb, player.position, moveSpeed);
        rb.AddForce(steeringForce);
        
        // LOS to player
        Vector3 playerDirection = (player.position - transform.position).normalized;
        RaycastHit2D hit = Physics2D.Raycast(transform.position, playerDirection, viewDistance);
        bool playerHit = hit && hit.collider.CompareTag("Player");

        // Shoot player if in LOS
        shootCooldown.Tick(Time.deltaTime);
        if (playerHit && shootCooldown.Expired())
        {
            shootCooldown.Reset();
            GameObject bullet = Instantiate(bulletPrefab);
            bullet.transform.position = transform.position + playerDirection;
            bullet.GetComponent<Rigidbody2D>().velocity = playerDirection * 10.0f;
            Destroy(bullet, 1.0f);
        }
    }

    void Patrol()
    {
        // Seek nearest waypoint
        Vector3 steeringForce = Vector2.zero;
        steeringForce += Steering.Seek(rb, waypoints[waypoint].transform.position, moveSpeed);
        rb.AddForce(steeringForce);
    }

    
    void OnTriggerEnter2D(Collider2D collision)
    {
        // State dependent waypoint selection
        if(state == State.NEUTRAL)
            SetWaypointToNearest();
        else if(state == State.DEFENSIVE)
            SetWaypointToFarthestFromPlayer();
    }
    
    private void StateChange(State newState)
    {
        // Call single use functions when entering a new state
        if (state != newState)
        {
            state = newState;
            switch (state)
            {
                case State.NEUTRAL:
                    EnterNeutralState();
                    break;
    
                case State.OFFENSIVE:
                    EnterAttackState();
                    break;
                
                case State.DEFENSIVE:
                    EnterDefenseState();
                    break;
            }
        }
    }

    // Single execution functions on state change
    private void EnterAttackState()
    {
        Debug.Log("Attacking!");
        color = Color.red;
    }

    private void EnterDefenseState()
    {
        Debug.Log("Defending!");
        defenceTimer.Reset();
        color = Color.yellow;
        SetWaypointToFarthestFromPlayer();
    }
    
    private void EnterNeutralState()
    {
        Debug.Log("Chilling!");
        color = Color.green;
        SetWaypointToNearest();
    }

    // Waypoint utilities
    private void SetWaypointToFarthestFromPlayer()
    {
        float d = 0;
        int farthest = 0;
        for (int i = 0; i < waypoints.Length; i++)
        {
            if (i != waypoint)
            {
                float n = (waypoints[i].transform.position - player.transform.position).magnitude;
                if (n > d)
                {
                    d = n;
                    farthest = i;
                }
            }
        }

        lastWaypoint = waypoint;
        waypoint = farthest;
    }

    
    private void SetWaypointToNearest()
    {
        // Sets waypoint to nearest that isn't the current or last waypoint
        int[] nextWaypoints = new int[2];
        for (int j = 0, i = 0; i < waypoints.Length; i++)
        {
            if(i != lastWaypoint && i != waypoint && j < 2)
            {
                nextWaypoints[j] = i;
                j++;
            }
        }
        
        int nearest;
        if ((waypoints[nextWaypoints[0]].transform.position - transform.position).magnitude <
            (waypoints[nextWaypoints[1]].transform.position - transform.position).magnitude)
            nearest = nextWaypoints[0];
        else
            nearest = nextWaypoints[1];
        
        lastWaypoint = waypoint;
        waypoint = nearest;
    }
    
    
}
