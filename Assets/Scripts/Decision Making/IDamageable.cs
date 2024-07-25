using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IDamageable
{
    public bool isPlayer { get; }
    public int health { get; }
    public void Damage(int damage);
}
