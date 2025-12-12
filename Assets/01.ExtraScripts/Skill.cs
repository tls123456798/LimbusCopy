using System;
using System.Collections.Generic;
using UnityEngine;

public enum TargetScope { SingleEnemy, AllEnemies, Self, SingleAlly}

[System.Serializable]
public class Skill
{
    public string Id; // 스킬 고유 ID
    public string Name; // 스킬 이름
    public TargetScope Scope; // 타겟 범위
    public int BasePower; //기본 공격력에 더할 위력
    public int CoinCount; // 코인 개수
    public int CoinBonus; // 코인 성공 시 추가 위력
    public int Cooldown; // 스킬 쿨타임

    [Header("부과 효과")]
    // 이 스킬이 부여할 수 있는 상태 이상 목록
    public List<StatusEffect> EffectsToApply = new List<StatusEffect>();

    // 생성자
    public Skill(string id,string name, TargetScope scope, int power, int coinCount, int coinBonus, int cd)
    {
        Id = id;
        Name = name;
        Scope = scope;  
        BasePower = power;  
        CoinCount = coinCount;  
        CoinBonus = coinBonus;  
        Cooldown = cd;
    }
}
