using System;
using System.Collections.Generic;
using UnityEngine;

public enum TargetScope { SingleEnemy, AllEnemies, Self, SingleAlly, SinglePlayer}

[System.Serializable]
public class Skill
{
    public string Id; // 스킬 고유 ID
    public string Name; // 스킬 이름
    public TargetScope Scope; // 타겟 범위

    // 데미지 필드
    public int BasePower; //기본 공격력에 더할 위력
    public int CoinCount; // 코인 개수
    public int CoinBonus; // 코인 성공 시 추가 위력

    [Header("쿨타임")]
    public int MaxCooldown; // 스킬 쿨타임
    public int CurrentCooldown; // 현재 남은 쿨타임 (CombatManager에서 관리)

    [Header("합(Clash) 위력")]
    // 합 계산 시 사용되는 기본 위력 (BasePower와 동일하게 설정 가능)
    public int ClashBase;
    // 합 계산 시 코인 성공 시 추가되는 보너스 (CoinBonus와 동일하게 설정 가능)
    public int ClashCoinBonus;
    // 합에만 영향을 미치는 추가 보너스 값 (예: 스킬 레벨 보정, 코인 앞면의 추가 위력 등)
    public int ClashBonus;

    [Header("합 진행 상태")]
    public int CurrentCoinCount; // 현재 남은 코인 수 (파괴 시 감소)
    public int WinCoinCount = 0; // 현재까지 합에서 이긴 코인 수

    [Header("부과 효과")]
    // 이 스킬이 부여할 수 있는 상태 이상 목록
    public List<StatusEffect> EffectsToApply = new List<StatusEffect>();

    // 생성자
    public Skill(string id,string name, TargetScope scope, 
        int power, int coinCount = 1, int coinBonus = 3, int cd = 0,
        int clashBase = -1, int clashBonus = 0) // 합 관련 인자 추가
    {
        Id = id;
        Name = name;
        Scope = scope;
        
        // 데미지 필드
        BasePower = power;  
        CoinCount = coinCount;  
        CoinBonus = coinBonus;  
        
        // 쿨타임
        MaxCooldown = cd;
        CurrentCooldown = 0; // 시작 시 쿨타임은 0

        // [합 필드 초기화]
        // ClashBase가 -1이면 BasePower와 동일하게 설정 (유연성 확보)
        ClashBase = (clashBase == -1) ? power : clashBase;
        ClashCoinBonus = coinBonus; // 합 코인 보너스는 데미지 코인 보너스와 동일하게 설정
        ClashBonus = clashBonus;

        // EffectsToApply 리스트 초기화 (null 방지)
        EffectsToApply = new List<StatusEffect>();
    }
    //public Skill(string name, int basePower, TargetScope scope)
    //{
    //    Id = name;
    //    Name = name;
    //    BasePower = basePower;
    //    Scope = scope;

    //    CoinCount = 1;
    //    CoinBonus = 3;
    //    MaxCooldown = 0;

    //    // [ 합 필드 초기화]
    //    ClashBase = basePower; // 합 기본 위력은 BasePower 와 동일
    //    ClashCoinBonus = 3; // 합 코인 보너스 기본값 3
    //    ClashBonus = 0; // 합 추가 보너스 없음
    //}
}
