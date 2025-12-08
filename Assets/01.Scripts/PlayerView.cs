using System;
using TMPro;
using UnityEngine;

public class PlayerView : MonoBehaviour
{
    [SerializeField] private TMP_Text healthText;
    [SerializeField] private SpriteRenderer spriteRenderer;
    public int MaxHealth { get; private set; }
    public int CurrentHealth { get; private set; }


    protected void SetupBase(int health, Sprite image)
    {
        MaxHealth = CurrentHealth = health;
        spriteRenderer.sprite = image;
        UPdateHealthText();
    }

    internal void Setup(PlayerData playerData)
    {
        throw new NotImplementedException();
    }

    private void UPdateHealthText()
    {
        healthText.text = "HP:" + CurrentHealth;
    }
}
