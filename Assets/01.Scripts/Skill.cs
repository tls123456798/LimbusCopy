using System.Collections.Generic;
using UnityEditor.UI;
using UnityEngine;

public class Skill
{
    public string Name => data.name;
    public string Description => data.Description;
    public Sprite Image => data.Image;
    public List<Effect> Effects => data.Effects;
    private readonly SkillData data;

    public Skill(SkillData skillData)
    {
        data = skillData;
    }
}
