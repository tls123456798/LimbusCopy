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
    public int Speed;


    [Header("기본 스탯")]
    // 버프/디버프 복원을 위한 기봇 스탯 저장
    public int BaseAttack;
    public int BaseDefense;

    // 계산된 능력치 속성
    public int Attack
    {
        get
        {
            int calculatedAttack = BaseAttack;

            // Attack을 변경하는 StatusEffect를 순회하며 최종 공격력 계산
            foreach(var effect in ActiveEffects.Where(e=> e.TargetStat == StatType.Attack))
            {
                // 일반적으로 버프/디버프 값은 합산됨 (가장 단순한 모델)
                calculatedAttack += (int)effect.Value;
            }
            // 최종 공격력이 최소 0이 되도록 보장
            return Mathf.Max(0, calculatedAttack);
        }
    }
    public int Defense
    {
        get
        {
            int calculatedDefense = BaseDefense;

            // Defense를 변경하는 StatusEffect를 순회하며 최종 방어력 계산
            foreach(var effect in ActiveEffects.Where(e=> e.TargetStat == StatType.Defense))
            {
                calculatedDefense += (int)effect.Value;
            }
            return Mathf.Max(0,calculatedDefense);
        }
    }
    // 스킬 및 상태 관리
    public List<Skill> AvailableSkills = new List<Skill>();
    public Dictionary<string, int> SkillCooldowns = new Dictionary<string, int>();
    public List<StatusEffect> ActiveEffects = new List<StatusEffect>();

    public string Name { get; set; }

    // 생성자
    public CharacterStats(string id, int maxHP, int attack, int speed, int defense)
    {
        Id = id;
        MaxHP = maxHP;
        CurrentHP = maxHP;
        Speed = speed;

        // Id를 Name에도 설정 (CombatManager에서 로그 출력을 위해 필요)
        Name = id;

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
        Skill basicAttack = new Skill(
            id: "basicAttack",
            name: "기본 공격",
            scope: TargetScope.SingleEnemy,
            basePower: 5,
            coinPower: 3,
            maxCoinCount: 1,
            cd: 0); // 쿨타임

        if(!AvailableSkills.Any(s => s.Id == basicAttack.Id))
        {
            AvailableSkills.Add(basicAttack);
            SkillCooldowns.Add(basicAttack.Id, 0);
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
