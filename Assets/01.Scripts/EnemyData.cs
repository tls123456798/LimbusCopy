using UnityEngine;

[CreateAssetMenu(menuName = "Data/Enemy")]
public class EnemyData : ScriptableObject
{
    [field: SerializeField] public string Name { get; private set; } // 캐릭터의 이름
    [field: SerializeField] public float CurrentHealth { get; private set; } // 현재 체력
    [field: SerializeField] public float MaxHealth { get; private set; } // 최대 체력
    [field: SerializeField] public int Speed { get; private set; } // 속도 (턴 순서 결정)
    [field: SerializeField] public Sprite Image { get; private set; } // 캐릭터의 이미지)
}
