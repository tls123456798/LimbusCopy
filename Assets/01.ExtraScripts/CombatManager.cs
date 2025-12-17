using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Collections;

public class CombatManager : MonoBehaviour
{
    public static CombatManager Instance { get; private set; }
    private UIController uiController;

    [Header("캐릭터 목록")]
    public List<CharacterStats> party = new List<CharacterStats>();
    public List<CharacterStats> enemies = new List<CharacterStats>();

    [Header("턴 관리")]
    private CombatState currentState = CombatState.Setup;
    private Queue<CharacterStats> turnOrderQueue = new Queue<CharacterStats>();
    public CharacterStats currentActor;

    [Header("입력 및 선택된 정보")]
    public Skill selectedSkill;
    public CharacterStats selectedTarget;
    private Skill targetClashSkill;

    [Header("전투 속도 설정")]
    public float clashDisplayDuration = 1.0f;

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;
    }

    void Start()
    {
        // 최신 유니티 버전 권장 API 사용
        uiController = FindFirstObjectByType<UIController>();
        if (uiController == null)
        {
            Debug.LogError("UIController를 찾을 수 없습니다.");
            return;
        }

        // 초기 데이터 세팅 (사용자 기존 코드 유지)
        InitializeDummyData();
        SetState(CombatState.Setup);
    }

    private void InitializeDummyData()
    {
        CharacterStats player = new CharacterStats("Player", 100, 15, 10, 10);
        CharacterStats enemy = new CharacterStats("Enemy", 80, 15, 8, 8);

        Skill playerAttack = new Skill("basicAttack", "기본 공격", TargetScope.SingleEnemy, 5, 2, 3, 0, 5, 0);
        Skill enemyAttack = new Skill("basicAttack", "적 기본 공격", TargetScope.SinglePlayer, 5, 2, 3, 0, 5, 0);

        player.AvailableSkills.Add(playerAttack);
        enemy.AvailableSkills.Add(enemyAttack);

        LinkStatsToViews(player, "Player");
        LinkStatsToViews(enemy, "Enemy");

        party.Add(player);
        enemies.Add(enemy);
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
                view.IntializeView();
            }
        }
    }

    public void SetState(CombatState newState)
    {
        currentState = newState;
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
                {
                    if (CheckCombatEndAndAdvanceTurn()) yield break;

                    if (turnOrderQueue.Count == 0)
                    {
                        EndRoundCleanup();
                        PrepareTurnOrder();
                    }

                    if (turnOrderQueue.Count == 0) { SetState(CombatState.CombatEnd); yield break; }

                    currentActor = turnOrderQueue.Dequeue();
                    if (currentActor.CurrentHP <= 0) { SetState(CombatState.StartTurn); yield break; }

                    if (party.Contains(currentActor))
                    {
                        uiController.ShowSkillSelection(currentActor);
                        currentState = CombatState.WaitingForInput;
                        yield break;
                    }
                    else
                    {
                        SetState(CombatState.ClashSetup);
                    }
                }
                break;

            case CombatState.ClashSetup:
                {
                    if (enemies.Contains(currentActor))
                    {
                        selectedTarget = party.FirstOrDefault(p => p.CurrentHP > 0);
                        selectedSkill = currentActor.AvailableSkills.FirstOrDefault(s => s.Id == "basicAttack");
                    }

                    if (selectedTarget != null)
                        targetClashSkill = selectedTarget.AvailableSkills.FirstOrDefault(s => s.Id == "basicAttack");

                    if (targetClashSkill == null || selectedSkill == null)
                    {
                        SetState(CombatState.CalculatingCombat);
                        yield break;
                    }

                    selectedSkill.CurrentCoinCount = selectedSkill.CoinCount;
                    targetClashSkill.CurrentCoinCount = targetClashSkill.CoinCount;
                    selectedSkill.WinCoinCount = 0;
                    targetClashSkill.WinCoinCount = 0;

                    SetState(CombatState.ClashCalculation);
                }
                break;

            case CombatState.ClashCalculation:
                {
                    yield return StartCoroutine(ClashCoinLoopRoutine(currentActor, selectedSkill, selectedTarget, targetClashSkill));
                    SetState(CombatState.CalculatingCombat);
                }
                break;

            case CombatState.CalculatingCombat:
                {
                    if (selectedTarget.CurrentHP <= 0 && selectedSkill.WinCoinCount > 0) { OnAnimationFinished(); yield break; }

                    // 최종 승자 결정 및 데미지 적용
                    if (selectedSkill.WinCoinCount > 0)
                    {
                        int dmg = CalculateLimbusDamage(currentActor, selectedTarget, selectedSkill, selectedSkill.WinCoinCount);
                        selectedTarget.TakeDamage(dmg);
                        UpdateCombatVisuals(selectedTarget, currentActor, dmg);
                    }
                    else if (targetClashSkill.WinCoinCount > 0)
                    {
                        int dmg = CalculateLimbusDamage(selectedTarget, currentActor, targetClashSkill, targetClashSkill.WinCoinCount);
                        currentActor.TakeDamage(dmg);
                        UpdateCombatVisuals(currentActor, selectedTarget, dmg);
                    }

                    SetState(CombatState.PlayingAnimation);
                }
                break;

            case CombatState.PlayingAnimation:
                {
                    CharacterView view = FindView(currentActor);
                    if (view != null) view.PlayAttackAnimation();
                    else OnAnimationFinished();
                }
                break;

            case CombatState.CombatEnd:
                Debug.Log("전투 종료");
                break;
        }
    }

    private IEnumerator ClashCoinLoopRoutine(CharacterStats attacker, Skill aSkill, CharacterStats defender, Skill dSkill)
    {
        while (aSkill.CurrentCoinCount > 0 && dSkill.CurrentCoinCount > 0)
        {
            int p1 = CalculateClashPower(attacker, aSkill);
            int p2 = CalculateClashPower(defender, dSkill);

            if (uiController != null)
                uiController.ShowClashResult(attacker.Name, p1, defender.Name, p2, aSkill.CurrentCoinCount, dSkill.CurrentCoinCount);

            yield return new WaitForSeconds(clashDisplayDuration);

            if (p1 > p2) dSkill.CurrentCoinCount--;
            else if (p2 > p1) aSkill.CurrentCoinCount--;
            // 무승부(p1 == p2) 시 코인 차감 없음
        }

        aSkill.WinCoinCount = aSkill.CurrentCoinCount;
        dSkill.WinCoinCount = dSkill.CurrentCoinCount;
    }

    private int CalculateClashPower(CharacterStats character, Skill skill)
    {
        // GetStatValue 오류 해결을 위해 직접 필드 참조 (또는 stats.GetStatValue 구현 필요)
        int power = character.Attack + skill.ClashBase;
        for (int i = 0; i < skill.CurrentCoinCount; i++)
        {
            if (UnityEngine.Random.Range(0, 2) == 1) power += skill.ClashCoinBonus;
        }
        return power;
    }

    private int CalculateLimbusDamage(CharacterStats attacker, CharacterStats defender, Skill skill, int finalCoins)
    {
        int attackPower = attacker.Attack + skill.BasePower + (skill.CoinBonus * finalCoins);
        int baseDamage = attackPower - defender.Defense;
        return Mathf.Max(1, baseDamage);
    }

    private void UpdateCombatVisuals(CharacterStats victim, CharacterStats winner, int damage)
    {
        CharacterView vView = FindView(victim);
        if (vView != null)
        {
            vView.UpdateHealthBar();
            vView.ShowDamageText(damage, Color.red);
            vView.ShowClashDefeatEffect();
        }
        CharacterView wView = FindView(winner);
        if (wView != null) wView.ShowClashVictoryEffect();
    }

    public void OnSkillSelected(Skill skill, CharacterStats target)
    {
        if (currentState != CombatState.WaitingForInput) return;
        selectedSkill = skill;
        selectedTarget = target;
        SetState(CombatState.ClashSetup);
    }

    private void PrepareTurnOrder()
    {
        var all = party.Concat(enemies).Where(c => c.CurrentHP > 0).OrderByDescending(c => c.Speed).ToList();
        turnOrderQueue = new Queue<CharacterStats>(all);
    }

    public void OnAnimationFinished()
    {
        EndTurnCleanup();
        SetState(CombatState.StartTurn);
    }

    private void EndTurnCleanup()
    {
        // 상태 이상 처리 로직 (생략 가능)
    }

    private void EndRoundCleanup() { }

    private bool CheckCombatEndAndAdvanceTurn()
    {
        if (party.All(p => p.CurrentHP <= 0) || enemies.All(e => e.CurrentHP <= 0))
        {
            SetState(CombatState.CombatEnd);
            return true;
        }
        return false;
    }

    private CharacterView FindView(CharacterStats stats)
    {
        return FindObjectsByType<CharacterView>(FindObjectsSortMode.None).FirstOrDefault(v => v.stats == stats);
    }
}