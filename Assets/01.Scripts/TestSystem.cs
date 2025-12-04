using Microsoft.Unity.VisualStudio.Editor;
using UnityEngine;

public class TestSystem : MonoBehaviour
{
    [SerializeField] private HandView handView;
    [SerializeField] private SkillData skillData;
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Skill skill = new(skillData);
            SkillView skillView = SkillViewCreator.Instance.CreateSkillView(skill, transform.position, Quaternion.identity);
            StartCoroutine(handView.AddCard(skillView));
        }
    }
}
