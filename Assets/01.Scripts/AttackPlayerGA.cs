using UnityEngine;

public class AttackPlayerGA : GameAction
{
    public EnemyView Attacker {  get; private set; }
    public AttackPlayerGA(EnemyView attacker)
    {
        Attacker = attacker;
    }
}
