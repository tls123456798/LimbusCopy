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
        coin.sprite = skill.Image;
        imageSR.sprite = skill.Image;
    }
}
