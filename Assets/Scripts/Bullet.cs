using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    public bool playerOwned = false;
    void OnTriggerEnter2D(Collider2D collision)
    {
        IDamageable iD = collision.GetComponent<IDamageable>();
        if (iD != null)
        {
            if (iD.isPlayer != playerOwned)
            {
                iD.Damage(20);
                Destroy(gameObject);
            }
        }
        else
        {
            if(!collision.CompareTag("Waypoint"))
                Destroy(gameObject);
        }
    }
}
