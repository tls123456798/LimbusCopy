using System.Xml.Serialization;
using TMPro;
using UnityEditor;
using UnityEngine;

public class SkillView : MonoBehaviour
{
    [SerializeField] private TMP_Text title;
    [SerializeField] private TMP_Text description;
    [SerializeField] private SpriteRenderer coin;
    [SerializeField] private SpriteRenderer imageSR;
    [SerializeField] private GameObject wrapper;
    public Skill Skill { get; private set; }
    public void Setup(Skill skill)
    {
        Skill = skill;
        title.text = skill.Name;
        description.text = skill.Description;
        coin.sprite = coin.sprite;
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
