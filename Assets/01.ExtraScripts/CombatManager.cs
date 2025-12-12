using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;
using UnityEditor.Rendering.LookDev;
using Unity.VisualScripting;

// 🚨 주의: CombatState, CharacterStats, Skill, StatusEffect, CharacterView 타입이 정의되어 있어야 합니다.

// ===================================================
// CombatState Enum은 CombatState.cs 파일에 정의되어 있습니다.
// public enum CombatState { Setup, StartTurn, WaitingForInput, CalculatingCombat, PlayingAnimation, CombatEnd }
// ===================================================

public class CombatManager : MonoBehaviour
{
    // ===================================================
    // 1. 싱글톤 패턴 및 외부 참조
    // ===================================================
    public static CombatManager Instance { get; private set; }

    // UIController는 스킬 선택 UI를 제어합니다.
    private UIController uiController;

    // ===================================================
    // 2. 캐릭터 목록 및 턴 관리
    // ===================================================
    [Header("캐릭터 목록")]
    public List<CharacterStats> party = new List<CharacterStats>();
    public List<CharacterStats> enemies = new List<CharacterStats>();

    [Header("턴 관리")]
    private CombatState currentState = CombatState.Setup;
    private Queue<CharacterStats> turnOrderQueue = new Queue<CharacterStats>();
    public CharacterStats currentActor;

    // ===================================================
    // 3. 입력 및 선택된 정보
    // ===================================================
    private Skill selectedSkill;             // 선택된 Skill 객체 (UIController에서 전달)
    private CharacterStats selectedTarget;   // 선택된 타겟


    // ===================================================
    // 4. 초기화 및 Unity LifeCycle
    // ===================================================
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
    }

    void Start()
    {
        // UIController 인스턴스 참조 (필수)
        uiController = UIController.Instance;
        if (uiController == null)
        {
            Debug.LogError("UIController 인스턴스를 찾을 수 없습니다.");
            return;
        }

        // 🚨 임시 캐릭터 생성 및 스탯 설정 (ID, MaxHP, ATK, SPD, DEF 순서)
        CharacterStats player = new CharacterStats("Player", 100, 15, 10, 10);
        CharacterStats enemy = new CharacterStats("Enemy", 80, 20, 8, 8);

        party.Add(player);
        enemies.Add(enemy);

        // 🚨 [핵심 수정] Setup 상태로 진입하여 턴 순서를 준비
        SetState(CombatState.Setup);
    }

    // ===================================================
    // 5. 상태 전환 핵심 로직
    // ===================================================

    public void SetState(CombatState newState)
    {
        Debug.Log($"전투 상태 변경: {currentState} -> {newState}");
        currentState = newState;

        switch (currentState)
        {
            case CombatState.Setup:
                PrepareTurnOrder(); // 턴 순서 준비
                SetState(CombatState.StartTurn);
                break;

            case CombatState.StartTurn:
                if (CheckCombatEndAndAdvanceTurn())
                {
                    return; // 전투가 종료되었으면 종료
                }

                // 🚨 [수정] Queue empty 오류 방지: 큐가 비었을 때 PrepareTurnOrder()가 다시 호출되는지 확인
                if (turnOrderQueue.Count == 0)
                {
                    PrepareTurnOrder();
                    if(turnOrderQueue.Count == 0)
                    {
                        // 모든 캐릭터가 사망한 경우 (이전 CheckCombatEndandAdvanceTurn 에서 잡혀야 함)
                        SetState(CombatState.CombatEnd);
                        return;
                    }
                }

                currentActor = turnOrderQueue.Dequeue();

                // 플레이어 턴
                if (party.Contains(currentActor))
                {
                    // UIController에게 스킬 선택 UI 활성화 요청
                    uiController.ShowSkillSelection(currentActor);
                    SetState(CombatState.WaitingForInput);
                    return;
                }
                // 적 AI 턴 (임시 로직)
                else
                {
                    // AI 로직: 가장 HP가 높은 파티원을 타겟으로 설정
                    selectedTarget = party.FirstOrDefault(p => p.CurrentHP > 0);
                    selectedSkill = currentActor.AvailableSkills.FirstOrDefault(s => s.Id == "BasicAttack");

                    if (selectedSkill == null || selectedTarget == null)
                    {
                        Debug.LogWarning("적 AI 턴: 타겟 또는 스킬이 없어 턴을 넘깁니다.");
                        OnAnimationFinished(); // 턴 종료 및 다음 턴으로
                        return;
                    }

                    SetState(CombatState.CalculatingCombat);
                }
                break;

            case CombatState.WaitingForInput:
                // UIController의 OnSkillSelected 호출을 기다립니다.
                break;

            case CombatState.CalculatingCombat:
                if (selectedTarget == null || selectedTarget.CurrentHP <= 0)
                {
                    Debug.LogWarning("타겟이 유효하지 않아 다음 턴으로 넘깁니다.");
                    OnAnimationFinished(); // 바로 다음 턴 시작
                    break;
                }

                // 🚨 [수정] CalculateLimbusDamage 인자 3개 전달
                int totalDamage = CalculateLimbusDamage(currentActor, selectedTarget, selectedSkill);

                // 2. 계산된 데미지를 타겟 캐릭터 데이터에 반영
                selectedTarget.TakeDamage(totalDamage);
                CharacterView targetView = FindView(selectedTarget);
                if(targetView != null)
                {
                    targetView.UpdateHealthBar();
                }
                // 애니메이션 상태로 전환
                SetState(CombatState.PlayingAnimation);
                break;

            case CombatState.PlayingAnimation:
                CharacterView attackerView = FindView(currentActor);
                if (attackerView != null)
                {
                    attackerView.PlayAttackAnimation();
                }
                else
                {
                    OnAnimationFinished();
                }
                break;

            case CombatState.CombatEnd:
                Debug.Log("전투 종료!");
                // TODO: 승리/패배 화면 표시
                break;
        }
    }

    /// <summary>
    /// UIController에서 스킬 선택 및 타겟팅이 완료되면 호출됩니다.
    /// </summary>
    public void OnSkillSelected(Skill skill, CharacterStats target)
    {
        if (currentState != CombatState.WaitingForInput) return;

        selectedSkill = skill;
        selectedTarget = target;

        SetState(CombatState.CalculatingCombat);
    }

    // ===================================================
    // 6. 턴 로직 처리 메서드
    // ===================================================

    private void PrepareTurnOrder()
    {
        // 사망한 캐릭터를 제외하고 Speed를 기반으로 순서를 결정
        var allCharacters = party.Concat(enemies)
            .Where(c => c.CurrentHP > 0)
            .OrderByDescending(c => c.Speed)
            .ToList();

        turnOrderQueue = new Queue<CharacterStats>(allCharacters);
        Debug.Log($"턴 순서 결정됨. 총 {allCharacters.Count}명");
    }

    /// <summary>
    /// CharacterView의 애니메이션이 끝났을 때 호출되어 다음 턴으로 넘어갑니다.
    /// </summary>
    public void OnAnimationFinished()
    {
        // 턴 종료 시 쿨타임 및 상태 이상 정리
        EndTurnCleanup();
        SetState(CombatState.StartTurn);
    }

    private void EndTurnCleanup()
    {
        List<CharacterStats> allCharacters = party.Concat(enemies).Where(c => c.CurrentHP > 0).ToList();

        foreach (var stats in allCharacters)
        {
            // 1. 스킬 쿨타임 감소
            var skillsToUpdate = stats.SkillCooldowns.Keys.ToList();
            foreach (var skillName in skillsToUpdate)
            {
                if (stats.SkillCooldowns.ContainsKey(skillName) && stats.SkillCooldowns[skillName] > 0)
                {
                    stats.SkillCooldowns[skillName]--;
                }
            }

            // 2. 상태 이상 지속 시간 감소 및 DoT 효과 적용
            stats.ActiveEffects.RemoveAll(effect =>
            {
                // DoT (DamageOverTime) 효과 적용
                if (effect.TargetStat == StatType.DamageOverTime)
                {
                    stats.TakeDamage((int)effect.Value);
                    CharacterView view = FindView(stats);
                    if (view != null) view.UpdateHealthBar();
                }

                effect.Duration--;

                if (effect.Duration <= 0)
                {
                    // 🚨 [완성] 버프/디버프가 끝날 때 스탯을 기본 스탯으로 복원
                    if (effect.TargetStat == StatType.Attack)
                    {
                        stats.Attack = stats.BaseAttack;
                    }
                    else if (effect.TargetStat == StatType.Defense)
                    {
                        stats.Defense = stats.BaseDefense;
                    }
                    // TODO: 다른 스탯(Speed 등)에 대한 복원 로직도 필요하다면 여기에 추가

                    // 지속 시간이 끝났으므로 제거
                    return true;
                }
                return false; // 유지
            });
        }
    }

    private bool CheckCombatEndAndAdvanceTurn()
    {
        // 파티 또는 적 전멸 여부 확인
        if (party.All(p => p.CurrentHP <= 0) || enemies.All(e => e.CurrentHP <= 0))
        {
            SetState(CombatState.CombatEnd);
            return true;
        }
        return false;
    }

    // ===================================================
    // 7. 데미지 계산 및 상태 이상 적용
    // ===================================================

    /// <summary>
    /// 최종 데미지를 계산합니다. (속성 시스템 제거)
    /// </summary>
    private int CalculateLimbusDamage(CharacterStats attacker, CharacterStats defender, Skill skill)
    {
        if (skill == null)
        {
            Debug.LogError("스킬 데이터가 누락되었습니다.");
            return 1;
        }

        // 1. 공격력 (Power) 계산 
        int attackPower = CalculateLimbusPower(
            attacker.Attack,
            skill.BasePower,
            skill.CoinCount,
            skill.CoinBonus
        );

        // 2. 기본 데미지 계산 (공격력 - 방어력)
        int baseDamage = attackPower - defender.Defense;

        // 🚨 [제거] 속성 상성 계수 로직 제거

        int finalDamage = baseDamage;

        // 3. 스킬 부가 효과 적용 (상태 이상 부여)
        ApplySkillEffects(skill, defender);

        // 최소 데미지는 1
        return Mathf.Max(1, finalDamage);
    }

    /// <summary>
    /// 공격자의 ATK, 스킬의 기본 위력 및 코인을 기반으로 최종 공격 위력을 계산합니다.
    /// </summary>
    private int CalculateLimbusPower(int attackerAttackStat, int basePower, int coinCount, int coinBonus)
    {
        int finalPower = attackerAttackStat + basePower;

        // 코인 던지기 (50% 성공률 가정)
        for (int i = 0; i < coinCount; i++)
        {
            if (UnityEngine.Random.Range(0, 2) == 1) // 1일 때 성공
            {
                finalPower += coinBonus;
            }
        }
        return finalPower;
    }

    /// <summary>
    /// 스킬에 포함된 상태 이상 효과를 타겟에게 적용합니다.
    /// </summary>
    private void ApplySkillEffects(Skill skill, CharacterStats target)
    {
        if (skill.EffectsToApply != null)
        {
            foreach (var effect in skill.EffectsToApply)
            {
                // 새로운 StatusEffect 인스턴스를 생성하여 리스트에 추가
                target.ActiveEffects.Add(new StatusEffect(
                    effect.Name,
                    effect.Type,
                    effect.TargetStat,
                    effect.Duration,
                    effect.Value
                ));
            }
        }
    }

    // ===================================================
    // 8. 유틸리티
    // ===================================================

    private CharacterView FindView(CharacterStats stats)
    {
        // 씬에서 CharacterView를 찾아 stats가 일치하는 View를 반환
        return FindObjectsOfType<CharacterView>().FirstOrDefault(v => v.stats == stats);
    }
}