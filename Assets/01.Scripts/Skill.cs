using UnityEditor.UI;
using UnityEngine;

public class Skill
{
    public string Name => data.name;
    public string Description => Description;
    public Sprite Image => data.Image;
    private readonly SkillData data;

    public Skill(SkillData skillData)
    {
        data = skillData;
    }
}
