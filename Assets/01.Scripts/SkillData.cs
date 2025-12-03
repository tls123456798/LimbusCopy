using UnityEngine;

[CreateAssetMenu(menuName = "Data/Skill")]
public class SkillData : ScriptableObject
{
    [field: SerializeField] public string SkillName { get; private set; } // 스킬 이름
    [field: SerializeField] public string Description {  get; private set; } // 스킬 효과
    [field: SerializeField] public Sprite Image { get; private set; } // 스킬 이미지
}
