using UnityEngine;
using System.Collections.Generic;

[System.Serializable] // 유니티 인스펙터에 표시되도록 설정

public class CharacterStats
{
    // 캐릭터의 고유 ID (어떤 캐릭터인지 식별)
    public string Id;

    // 기본 능력치
    public int MaxHP;
    public int CurrentHP;
    public int Attack;
    public int Speed; // 턴 순서 결정에 사용

    // 스킬 정보 (코인, 위력 등을 담을 구조체)
    public List<string> Skills;

    // 생성자 (Constructor)
    public CharacterStats(string id, int hp, int atk, int spd)
    {
        this.Id = id;
        this.MaxHP = hp;
        this.CurrentHP = hp;
        this.Attack = atk;
        this.Speed = spd;
    }
    // 메서드 예시: 데미지 받기
    public void TakeDamage(int damage)
    {
        CurrentHP -= damage;
        if (CurrentHP < 0) CurrentHP = 0;

        UnityEngine.Debug.Log($"{damage} 데미지를 받았습니다. 남은 HP: {CurrentHP}");
    }
}
