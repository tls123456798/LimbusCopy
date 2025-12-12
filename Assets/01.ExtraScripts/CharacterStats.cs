using UnityEngine;
using System.Collections.Generic;
using Unity.VisualScripting.Antlr3.Runtime;
using System.Linq;

[System.Serializable] // 유니티 인스펙터에 표시되도록 설정

public class CharacterStats
{
    // 기본 능력치
    public string Id;
    public int MaxHP;
    public int CurrentHP;

    public int Attack;
    public int Speed;
    public int Defense;

    [Header("기본 스탯")]
    // 버프/디버프 복원을 위한 기봇 스탯 저장
    public int BaseAttack;
    public int BaseDefense;

    // 스킬 및 상태 관리
    public List<Skill> AvailableSkills = new List<Skill>();
    public Dictionary<string, int> SkillCooldowns = new Dictionary<string, int>();
    public List<StatusEffect> ActiveEffects = new List<StatusEffect>();

    // 생성자
    public CharacterStats(string id, int maxHP, int attack, int speed, int defense)
    {
        Id = id;
        MaxHP = maxHP;
        Attack = attack;
        Speed = speed;
        Defense = defense;

        // 기본 스탯 저장
        BaseAttack = attack;
        BaseDefense = defense;

        // 필드 초기화
        AvailableSkills = new List<Skill>();
        SkillCooldowns = new Dictionary<string, int>();
        ActiveEffects = new List<StatusEffect>();

        // 기본 스킬 초기화 호출
        InitializeBaseSkills();
    }
    private void InitializeBaseSkills()
    {
        // Skill("Id", "Name", Scope, BasePower, CoinCount, CoinBonus, Cooldown)
        Skill basicAttack = new Skill("basicAttack", "기본 공격", TargetScope.SingleEnemy,
            /* Power */ 5, /* CoinCount */ 1, /* CoinBonus */ 3, /* Cooldown */ 0);
        if(!AvailableSkills.Any(s => s.Id == basicAttack.Id))
        {
            AvailableSkills.Add(basicAttack);
            SkillCooldowns.Add(basicAttack.Id,0);
        }
    }
    // 핵심 전투 메서드
    public void TakeDamage(int damage)
    {
        if (damage <0) damage = 0;
        CurrentHP = Mathf.Max(0, CurrentHP - damage); // 0 미만 방지

        if(CurrentHP == 0)
        {
            // 사망처리
        }
    }
}
