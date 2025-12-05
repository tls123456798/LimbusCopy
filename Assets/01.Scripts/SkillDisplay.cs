using UnityEngine;

public class SkillDisplay : MonoBehaviour
{
    [field: SerializeField] public string Skillname { get; private set; }
    [field: SerializeField] public string Description { get; private set; }
    [field: SerializeField] public Sprite Image { get; private set; }

    public void UpdataUI(SkillData data)
    {
        Skillname = data.SkillName;
        Description = data.Description;
        Image = data.Image;
    }
}
