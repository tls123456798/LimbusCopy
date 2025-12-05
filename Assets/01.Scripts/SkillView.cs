using System.Xml.Serialization;
using TMPro;
using UnityEditor;
using UnityEngine;

public class SkillView : MonoBehaviour
{
    [SerializeField] private TMP_Text title;
    [SerializeField] private TMP_Text description;
    [SerializeField] private SpriteRenderer imageSR;
    [SerializeField] private GameObject wrapper;
    [SerializeField] private SkillData skillData;

    public Skill Skill { get; private set; }
    public void Start()
    {
        transform.position = Vector3.zero;
    }
    public void Setup(Skill skill)
    {
        Skill = skill;
        title.text = skill.Name;
        description.text = skill.Description;
        imageSR.sprite = skill.Image;
    }
    private void OnMouseEnter()
    {
        wrapper.SetActive(false);
        Vector3 pos = new(transform.position.x, -2, 0);
        SkillViewHoverSystem.Instance.Show(Skill, pos);
    }
    private void OnMouseExit()
    {
        SkillViewHoverSystem.Instance.Hide();
        wrapper.SetActive(true);
    }
}
