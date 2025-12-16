using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Collections; // 코루틴 사용을 위해 추가

// 🚨 주의: CombatState, CharacterStats, Skill, StatusEffect, TargetScope, StatType, CharacterView, UIController 타입이 정의되어 있어야 합니다.

public class CombatManager : MonoBehaviour
{
    // ===================================================
    // 1. 싱글톤 패턴 및 외부 참조
    // ===================================================
    public static CombatManager Instance { get; private set; }
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
    public Skill selectedSkill;
    public CharacterStats selectedTarget;
    private Skill targetClashSkill; // 타겟의 대응 스킬

    [Header("전투 속도 설정")]
    public float clashDisplayDuration = 1.0f; // 합 결과 표시 시간

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
        uiController = FindObjectOfType<UIController>(); // UIController를 씬에서 찾도록 변경
        if (uiController == null)
        {
            Debug.LogError("UIController 인스턴스를 찾을 수 없습니다.");
            return;
        }

        // 임시 캐릭터 데이터 생성
        CharacterStats player = new CharacterStats("Player", 100, 15, 10, 10);
        CharacterStats enemy = new CharacterStats("Enemy", 80, 20, 8, 8);

        // 적 스킬 초기화 
        Skill enemyAttack = new Skill(
            id: "basicAttack", name: "적 기본 공격", scope: TargetScope.SinglePlayer,
            power: 5, coinCount: 1, coinBonus: 3, cd: 0, clashBase: 5, clashBonus: 0);

        enemy.AvailableSkills.Clear();
        enemy.AvailableSkills.Add(enemyAttack);


        LinkStatsToViews(player, "Player");
        LinkStatsToViews(enemy, "Enemy");

        party.Add(player);
        enemies.Add(enemy);

        SetState(CombatState.Setup);
    }

    private void LinkStatsToViews(CharacterStats stats, string objectName)
    {
        GameObject go = GameObject.Find(objectName);
        if (go != null)
        {
            CharacterView view = go.GetComponent<CharacterView>();
            if (view != null)
            {
                view.stats = stats;
                // 🚨 InitializeView 호출로 HP 바와 데이터 연결을 확실히 합니다.
                view.IntializeView();
                Debug.Log($"뷰 연결 성공: {stats.Id}");
            }
            else
            {
                Debug.LogError($"오류: 오브젝트'{objectName}'에 CharacterView 컴포넌트가 없습니다. ");
            }
        }
        else
        {
            Debug.LogError($"오류: Hierachy에서 오브젝트 '{objectName}'을 찾을 수 없습니다.");
        }
    }

    // ===================================================
    // 5. 상태 전환 핵심 로직 (Clash 로직 포함)
    // ===================================================

    public void SetState(CombatState newState)
    {
        Debug.Log($"전투 상태 변경: {currentState} -> {newState}");
        currentState = newState;

        // 상태 전환 시 코루틴을 사용하여 딜레이를 주어 흐름을 제어합니다.
        StartCoroutine(StateExecutionRoutine(newState));
    }

    private IEnumerator StateExecutionRoutine(CombatState state)
    {
        switch (state)
        {
            case CombatState.Setup:
                PrepareTurnOrder();
                SetState(CombatState.StartTurn);
                break;

            case CombatState.StartTurn:
                { // 🚨 CS0136 오류 방지를 위해 스코프 추가
                    if (CheckCombatEndAndAdvanceTurn())
                    {
                        yield break;
                    }

                    if (turnOrderQueue.Count == 0)
                    {
                        EndRoundCleanup(); // 라운드 종료 정리 로직 호출
                        PrepareTurnOrder();
                        if (turnOrderQueue.Count == 0)
                        {
                            SetState(CombatState.CombatEnd);
                            yield break;
                        }
                    }

                    currentActor = turnOrderQueue.Dequeue();

                    // 사망한 캐릭터는 턴을 넘깁니다.
                    if (currentActor.CurrentHP <= 0)
                    {
                        Debug.Log($"[{currentActor.Name}]은 사망하여 턴을 넘깁니다.");
                        SetState(CombatState.StartTurn);
                        yield break;
                    }

                    // 플레이어 턴 (입력 대기)
                    if (party.Contains(currentActor))
                    {
                        uiController.ShowSkillSelection(currentActor);
                        currentState = CombatState.WaitingForInput; // SetState 대신 직접 상태 변경 (입력 대기)
                        yield break; // 입력이 들어올 때까지 대기
                    }
                    // 적 AI 턴 (즉시 합 준비 상태로 전환)
                    else if (enemies.Contains(currentActor))
                    {
                        SetState(CombatState.ClashSetup);
                    }
                    break;
                } // 🚨 스코프 종료

            case CombatState.WaitingForInput:
                // OnSkillSelected 호출을 기다립니다.
                break;

            case CombatState.ClashSetup:
                { // 🚨 CS0136 오류 방지를 위해 스코프 추가
                    Debug.Log($"[{currentActor.Name}] Clash Setup 시작.");

                    if (enemies.Contains(currentActor))
                    {
                        selectedTarget = party.FirstOrDefault(p => p.CurrentHP > 0);
                        selectedSkill = currentActor.AvailableSkills.FirstOrDefault(s => s.Id == "basicAttack");

                        if (selectedSkill == null || selectedTarget == null)
                        {
                            Debug.LogWarning("적 AI 턴: 타겟 또는 스킬이 없어 턴을 넘깁니다.");
                            OnAnimationFinished();
                            yield break;
                        }
                    }

                    targetClashSkill = selectedTarget.AvailableSkills.FirstOrDefault(s => s.Id == "basicAttack");

                    if (targetClashSkill == null)
                    {
                        Debug.LogWarning($"타겟 [{selectedTarget.Name}]이 대응할 스킬이 없습니다. 일반 공격으로 진행.");
                        SetState(CombatState.CalculatingCombat); // 합 없이 일반 공격으로 진행
                        yield break;
                    }

                    SetState(CombatState.ClashCalculation);
                    break;
                } // 🚨 스코프 종료

            case CombatState.ClashCalculation:
                { // 🚨 CS0136 오류 방지를 위해 스코프 추가
                    // 1. 합 위력 계산
                    int actorPower = CalculateClashPower(currentActor, selectedSkill);
                    int targetPower = CalculateClashPower(selectedTarget, targetClashSkill);

                    // 🚨 3. UI/시각화 호출 (UIController에 ShowClashResult가 정의되어야 함)
                    if (uiController != null)
                    {
                        // uiController.ShowClashResult(currentActor.Name, actorPower, selectedTarget.Name, targetPower); // 🚨 CS1061 오류 방지를 위해 임시 주석 처리
                        Debug.Log($"합 UI 호출 시도: {currentActor.Name}({actorPower}) vs {selectedTarget.Name}({targetPower})");
                    }

                    yield return new WaitForSeconds(clashDisplayDuration); // 합 결과 표시를 위한 딜레이

                    // 2. 승패 판정 및 데미지 로직 전환
                    if (actorPower > targetPower)
                    {
                        // 합 승리: 공격자 턴이 CalculatingCombat으로 이어집니다.
                        CharacterView attackerView = FindView(currentActor); // 🚨 로컬 변수 재정의
                        if (attackerView != null) attackerView.ShowClashVictoryEffect(); // 합 승리 임시 효과

                        SetState(CombatState.CalculatingCombat);
                    }
                    else // 무승부/패배 처리 (타겟 승리 또는 무승부)
                    {
                        // 합 패배/무승부: 타겟이 반격합니다.
                        CharacterStats winner = selectedTarget;
                        CharacterStats loser = currentActor;
                        Skill winnerSkill = targetClashSkill;

                        int totalDamage = CalculateLimbusDamage(winner, loser, winnerSkill); // 🚨 로컬 변수 재정의
                        loser.TakeDamage(totalDamage);

                        // HP 바 업데이트 및 시각화
                        CharacterView loserView = FindView(loser); // 🚨 로컬 변수 재정의
                        CharacterView winnerView = FindView(winner);

                        if (loserView != null)
                        {
                            loserView.UpdateHealthBar();
                            loserView.ShowDamageText(totalDamage, Color.red); // 피격자 데미지 표시
                            loserView.ShowClashDefeatEffect(); // 합 패배 임시 효과
                        }
                        if (winnerView != null) winnerView.ShowClashVictoryEffect(); // 반격 승리 임시 효과

                        // 딜레이 후 다음 턴으로
                        yield return new WaitForSeconds(clashDisplayDuration);
                        OnAnimationFinished();
                    }
                    break;
                } // 🚨 스코프 종료

            case CombatState.CalculatingCombat:
                { // 🚨 CS0136 오류 방지를 위해 스코프 추가
                    if (selectedTarget == null || selectedTarget.CurrentHP <= 0)
                    {
                        OnAnimationFinished();
                        yield break;
                    }

                    // 데미지 계산 및 반영
                    int totalDamage = CalculateLimbusDamage(currentActor, selectedTarget, selectedSkill); // 🚨 로컬 변수 재정의
                    selectedTarget.TakeDamage(totalDamage);

                    // HP 바 업데이트 및 시각화
                    CharacterView targetView = FindView(selectedTarget);
                    if (targetView != null)
                    {
                        targetView.UpdateHealthBar();
                        targetView.ShowDamageText(totalDamage, Color.red); // 데미지 표시
                    }

                    SetState(CombatState.PlayingAnimation);
                    break;
                } // 🚨 스코프 종료

            case CombatState.PlayingAnimation:
                { // 🚨 CS0136 오류 방지를 위해 스코프 추가
                    CharacterView attackerView = FindView(currentActor); // 🚨 로컬 변수 재정의
                    if (attackerView != null)
                    {
                        // 공격 애니메이션 재생 (뷰 내부에서 OnAnimationFinished 호출)
                        attackerView.PlayAttackAnimation();
                    }
                    else
                    {
                        // 뷰가 없으면 바로 턴 종료
                        OnAnimationFinished();
                    }
                    // PlayAttackAnimation에서 코루틴이 시작되므로 여기서 yield break
                    yield break;
                } // 🚨 스코프 종료

            case CombatState.CombatEnd:
                Debug.Log("전투 종료!");
                yield break;
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

        SetState(CombatState.ClashSetup);
    }

    // ===================================================
    // 6. 턴 로직 처리 메서드
    // ===================================================

    private void PrepareTurnOrder()
    {
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
        EndTurnCleanup();
        SetState(CombatState.StartTurn);
    }

    /// <summary>
    /// 캐릭터의 개별 턴 종료 후 정리 로직입니다.
    /// </summary>
    private void EndTurnCleanup()
    {
        // 쿨타임 감소 및 상태 이상 지속 시간 감소는 현재 행동한 캐릭터에게만 적용합니다.

        // 1. 스킬 쿨타임 감소
        foreach (var skill in currentActor.AvailableSkills)
        {
            // 🚨 Skill 클래스에 CurrentCooldown 변수가 필요합니다.
            // if (currentActor.SkillCooldowns.TryGetValue(skill.Id, out int cd))
            // {
            //     currentActor.SkillCooldowns[skill.Id] = Mathf.Max(0, cd - 1);
            // }
        }

        // 2. 상태 이상 지속 시간 감소 및 DoT 효과 적용
        // 상태 이상 제거 시 ActiveEffects 리스트를 순회하는 것이 안전합니다.
        for (int i = currentActor.ActiveEffects.Count - 1; i >= 0; i--)
        {
            StatusEffect effect = currentActor.ActiveEffects[i];

            // DoT 적용
            if (effect.TargetStat == StatType.DamageOverTime)
            {
                currentActor.TakeDamage((int)effect.Value);
                CharacterView view = FindView(currentActor);
                if (view != null)
                {
                    view.UpdateHealthBar();
                    view.ShowDamageText((int)effect.Value, Color.magenta); // DoT 데미지는 다른 색
                }
            }

            effect.Duration--;

            if (effect.Duration <= 0)
            {
                currentActor.ActiveEffects.RemoveAt(i);
            }
        }
    }

    /// <summary>
    /// 라운드가 종료(모든 캐릭터가 턴을 마쳤을 때)되었을 때 호출됩니다.
    /// 여기에 특별한 로직을 구현할 수 있습니다.
    /// </summary>
    private void EndRoundCleanup()
    {
        // 모든 캐릭터에 걸린 상태 이상 턴 지속 시간 감소 등을 여기에 구현할 수 있습니다.
    }


    private bool CheckCombatEndAndAdvanceTurn()
    {
        if (party.All(p => p.CurrentHP <= 0))
        {
            Debug.Log("패배: 아군 전멸!");
            SetState(CombatState.CombatEnd);
            return true;
        }
        if (enemies.All(e => e.CurrentHP <= 0))
        {
            Debug.Log("승리: 적군 전멸!");
            SetState(CombatState.CombatEnd);
            return true;
        }
        return false;
    }

    // ===================================================
    // 7. 데미지 계산, 상태 이상 및 합 위력 계산
    // ===================================================

    /// <summary>
    /// 캐릭터와 스킬을 기반으로 최종 합 위력을 계산합니다.
    /// </summary>
    private int CalculateClashPower(CharacterStats character, Skill skill)
    {
        if (skill == null) return 0;

        // 1. 기본 위력 (Attack Stat + ClashBase)
        int finalPower = character.Attack + skill.ClashBase;

        // 2. 코인 던지기 (코인 개수와 ClashCoinBonus 사용)
        for (int i = 0; i < skill.CoinCount; i++)
        {
            if (UnityEngine.Random.Range(0, 2) == 1)
            {
                finalPower += skill.ClashCoinBonus;
            }
        }

        Debug.Log($"[{character.Name}] 합 위력 결과: {finalPower}");
        return finalPower;
    }

    /// <summary>
    /// 최종 데미지를 계산합니다.
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

        int finalDamage = baseDamage;

        // 3. 스킬 부가 효과 적용 (상태 이상 부여)
        ApplySkillEffects(skill, defender);

        // 최소 데미지는 1
        return Mathf.Max(1, finalDamage);
    }

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

    private void ApplySkillEffects(Skill skill, CharacterStats target)
    {
        if (skill.EffectsToApply != null)
        {
            foreach (var effect in skill.EffectsToApply)
            {
                // StatusEffect를 새로 복사하여 추가해야 중복/지속 시간 관리가 가능합니다.
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
        // 🚨 FindObjectsOfType 대신 성능을 위해 씬 시작 시 미리 캐싱하는 것이 좋습니다.
        return FindObjectsOfType<CharacterView>().FirstOrDefault(v => v.stats == stats);
    }
}