using UnityEngine;

public class EndTurnButtonUI : MonoBehaviour
{
    public void OnClck()
    {
        EnemyTurnGA enemyTurnGA = new();
        ActionSystem.Instance.Perform(enemyTurnGA);
    }
}
