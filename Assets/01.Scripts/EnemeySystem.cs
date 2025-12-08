using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemeySystem : Singleton<EnemeySystem>
{
    internal static object Instance;

    private void OnEnable()
    {
        ActionSystem.AttachPerformer<EnemyTurnGA>(EnemyTurnPerformer);
    }
    private void OnDisable()
    {
        ActionSystem.DetachPerformer<EnemyTurnGA>();
    }
  private IEnumerator EnemyTurnPerformer(EnemyTurnGA enemyTurnGA)
    {
        Debug.Log("Enemy Turn");
        yield return new();
        Debug.Log("End Enemy Turn");
    }
}
