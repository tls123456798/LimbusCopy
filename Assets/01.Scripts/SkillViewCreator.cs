using DG.Tweening;
using UnityEngine;

public class SkillViewCreator : Singleton<SkillViewCreator>
{
    [SerializeField] private SkillView skillViewPrefab;
    public SkillView CreateSkillView(Skill skill, Vector3 position, Quaternion rotation)
    {
        SkillView skillView = Instantiate(skillViewPrefab, position, rotation);
        skillView.transform.localScale = Vector3.zero;
        skillView.transform.DOScale(Vector3.one, 0.15f);
        skillView.Setup(skill);
        return skillView;
    }
}
