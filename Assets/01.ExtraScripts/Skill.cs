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
    public int CoinPower; // 코인 위력 (앞면 하나당 추가되는 수치)
    public int MaxCoinCount; // 최대 코인 개수

    [Header("쿨타임")]
    public int MaxCooldown; // 스킬 쿨타임
    public int CurrentCooldown; // 현재 남은 쿨타임 (CombatManager에서 관리)

    [Header("합 진행 상태")]
    public int CurrentCoinCount; // 현재 남은 코인 수 (파괴 시 감소)
    public int WinCoinCount = 0; // 현재까지 합에서 이긴 코인 수

    [Header("부과 효과")]
    // 이 스킬이 부여할 수 있는 상태 이상 목록
    public List<StatusEffect> EffectsToApply = new List<StatusEffect>();

    // 생성자
    public Skill(string id, string name, TargetScope scope, int basePower,
        int coinPower, int maxCoinCount, int cd = 0)
    {
        Id = id;
        Name = name;
        Scope = scope;
        BasePower = basePower;
        CoinPower = coinPower;
        MaxCoinCount = maxCoinCount;

        // 초기화 시 현재 코인은 최대 코인과 동일
        CurrentCoinCount = maxCoinCount;

        MaxCooldown = cd;
        CurrentCooldown = 0;
        EffectsToApply = new List<StatusEffect>();
    }
    /// <summary>
    /// 합 또는 공격 시작 전 코인 개수를 최대치로 리셋합니다.
    /// </summary>
    public void ResetCoinCount()
    {
        CurrentCoinCount = MaxCoinCount;
    }
    /// <summary>
    /// 현재 남은 코인 개수만큼 던져서 최종 위력을 계산합니다.
    /// </summary>
    /// <returns>최종 위력 값 및 앞면 개수(연출용)</returns>
    public (int finalPower, int headsCount) GetExecutionResult()
    {
        int headsCount = 0;

        // 현재 남은 코인 수만큼만 던짐 (코인 파괴 반영)
        for(int i = 0; i < CurrentCoinCount; i++)
        {
            // 50% 확률로 앞면 (실제 게임처럼 정신력 시스템 추가 시 확률 변동 가능)
            if(UnityEngine.Random.value < 0.5f)
            {
                headsCount++;
            }
        }

        // 최종 위력 = 기본 위력 + (앞면 개수 * 코인 위력)
        // 만약 코인 위력이 마이너스인 스킬(침식 등) 도 처리 가능하도록 설계
        int finalPower = BasePower + (headsCount * CoinPower);

        // 위력은 최소 0 이하로 내려가지 않게 방어
        finalPower = Mathf.Max(0, finalPower);

        return (finalPower, headsCount);
    }
}
