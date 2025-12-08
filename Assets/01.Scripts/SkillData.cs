using SerializeReferenceEditor;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Data/Skill")]
public class SkillData : ScriptableObject
{
    [field: SerializeField] public string SkillName { get; private set; } // 스킬 이름
    [field: SerializeField] public string Description {  get; private set; } // 스킬 효과
    [field: SerializeField] public Sprite Image { get; private set; } // 스킬 이미지
    [field: SerializeReference, SR] public List<Effect> Effects { get; private set; } 

    [field: SerializeField] public int coinflipCount { get; private set; }
    [field: SerializeField] public float successChance = 0.5f;
    [field: SerializeField] public float bonusPerSuccess = 0.5f;

    public int baseDamage;
}
