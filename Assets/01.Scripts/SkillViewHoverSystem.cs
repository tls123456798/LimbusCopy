using UnityEngine;

public class SkillViewHoverSystem : Singleton<SkillViewHoverSystem>
{
    [SerializeField] private SkillView skillViewHover;
    public void Show(Skill skill, Vector3 position)
    {
        skillViewHover.gameObject.SetActive(true);
        skillViewHover.Setup(skill);
        skillViewHover.transform.position = position;
    }
    public void Hide()
    {
        skillViewHover.gameObject.SetActive(false);
    }
}
