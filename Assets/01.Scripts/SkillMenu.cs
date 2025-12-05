using TMPro;
using UnityEditor.Build.Pipeline.Utilities;
using UnityEngine;

public class SkillMenu : MonoBehaviour
{
    public SkillData skillData;
    private GameObject SkillDescriptionPrefab;
    private GameObject currentSkillView;
    private Transform CoinTransform;


    private void Awake()
    {
        CoinTransform = transform;
        if (CoinTransform != null)
        {
            CoinTransform = transform;
        }
    }
    private void OnMouseEnter()
    {
        Skill skill = new(skillData);
        SkillView skillView = SkillViewCreator.Instance.CreateSkillView(skill, transform.position, Quaternion.identity);
        if (currentSkillView != null) return;
        currentSkillView = Instantiate(SkillDescriptionPrefab, CoinTransform);
        SkillDisplay display = currentSkillView.GetComponent<SkillDisplay>();
        if(display != null && skillData != null)
        {
            display.UpdataUI(skillData);
        }
    }
    private void OnMouseExit()
    {
        if(currentSkillView != null)
        {
            Destroy(currentSkillView);
            currentSkillView = null;
        }
    }
}
