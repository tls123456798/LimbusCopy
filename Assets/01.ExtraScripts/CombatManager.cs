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
    public float clashDisplayDuration = 0.8f;

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

        Skill playerAttack = new Skill("basicAttack", "기본 공격", TargetScope.SingleEnemy, 5, 3, 2, 0);
        Skill enemyAttack = new Skill("basicAttack", "적 기본 공격", TargetScope.SinglePlayer, 5, 3, 2, 0);

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
                    }
                    else
                    {
                        SetState(CombatState.ClashSetup);
                    }
                    break;
                }

            case CombatState.ClashSetup:
                {
                    if (enemies.Contains(currentActor))
                    {
                        selectedTarget = party.FirstOrDefault(p => p.CurrentHP > 0);
                        selectedSkill = currentActor.AvailableSkills.FirstOrDefault();
                    }

                    if (selectedTarget != null)
                        targetClashSkill = selectedTarget.AvailableSkills.FirstOrDefault();

                    // 합을 시작하기 전 코인 개수를 최대로 리셋 (Skill.cs에 추가한 메서드 활용)
                    if(selectedSkill != null) selectedSkill.ResetCoinCount();
                    if(targetClashSkill != null) targetClashSkill.ResetCoinCount();

                    SetState(CombatState.ClashCalculation);
                    break;
                }

            case CombatState.ClashCalculation:
                {
                    // 합 루프 실행
                    yield return StartCoroutine(ClashCoinLoopRoutine(currentActor, selectedSkill, selectedTarget, targetClashSkill));
                    SetState(CombatState.CalculatingCombat);
                    break;
                }

            case CombatState.CalculatingCombat:
                {
                    // 최종 승자 확인 및 데임지 단계
                    if(selectedSkill.CurrentCoinCount > 0)
                    {
                        // 아군 (또는 현재 공격자) 승리
                        int dmg = CalculateFinalDamage(currentActor, selectedSkill);
                        selectedTarget.TakeDamage(dmg);
                        UpdateCombatVisuals(selectedTarget, currentActor,dmg);
                        SetState(CombatState.PlayingAnimation);
                    }
                    else if(targetClashSkill.CurrentCoinCount > 0)
                    {
                        // 적군(또는 피격자) 승리 (합에서 이겨서 반격)
                        int dmg = CalculateFinalDamage(selectedTarget, targetClashSkill);
                        currentActor.TakeDamage(dmg);
                        UpdateCombatVisuals(currentActor,selectedTarget,dmg);
                        SetState(CombatState.PlayingAnimation);
                    }
                    else
                    {
                        // 무승부 등으로 인해 둘 다 코인이 없으면 다음 턴으로
                        OnAnimationFinished();
                    }
                    break;
                }

            case CombatState.PlayingAnimation:
                {
                    CharacterView view = FindView(currentActor); // 실제로는 승리자 뷰를 찾아야 함
                    if (view != null) view.PlayAttackAnimation();
                    else OnAnimationFinished();
                    break;
                }

            case CombatState.CombatEnd:
                {
                    // 모든 적이 죽었는지 확인
                    bool playerWon = enemies.All(e => e.CurrentHP <= 0);

                    // 애니메이션이 끝날 시간을 고려하여 약간의 딜레이 후 UI 표시
                    StartCoroutine(ShowResultWithDelay(playerWon));
                    break;
                }
        }
    }
    private IEnumerator ClashCoinLoopRoutine(CharacterStats aStats, Skill aSkill, CharacterStats dStats, Skill dSkill)
    {
        // 누군가의 코인이 0이 될 때까지 무한 반복
        while (aSkill.CurrentCoinCount > 0 && dSkill.CurrentCoinCount > 0)
        {
            // Skill.cs 의 신규 메서드로 위력 계산
            var aRes = aSkill.GetExecutionResult();
            var dRes = dSkill.GetExecutionResult();

            uiController.ShowClashResult(aStats.Name, aRes.finalPower, dStats.Name, dRes.finalPower, aSkill.CurrentCoinCount, dSkill.CurrentCoinCount);
            yield return new WaitForSeconds(clashDisplayDuration);

            // 위력 비교 후 패배자 코인 파괴
            if(aRes.finalPower > dRes.finalPower)
            {
                dSkill.CurrentCoinCount--;
                Debug.Log($"{dStats.Name}의 코인 파괴! 남은 코인: {dSkill.CurrentCoinCount}");
            }
            else if (dRes.finalPower > aRes.finalPower)
            {
                aSkill.CurrentCoinCount--;
                Debug.Log($"{dStats.Name}의 코인 파괴! 남은 코인: {dSkill.CurrentCoinCount}");
            }
            // 무승부 시 아무 일도 일어나지 않고 다시 Loop
        }
    }
    private int CalculateFinalDamage(CharacterStats attacker, Skill skill)
    {
        // 승리 시점의 남은 코인 개수로 최종 데미지 계산
        var result = skill.GetExecutionResult();
        int totalDamage = attacker.Attack + result.finalPower;
        return Mathf.Max(1, totalDamage);
    }
    private void UpdateCombatVisuals(CharacterStats victim, CharacterStats winner, int damage)
    {
        CharacterView vView = FindView(victim);
        if (vView != null)
        {
            vView.UpdateHealthBar();
            vView.ShowDamageText(damage, Color.red);
        }
    }
    public void OnSkillSelected(Skill skill, CharacterStats target)
    {
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
        SetState(CombatState.StartTurn);
    }
    private void EndRoundCleanup()
    {

    }
    private bool CheckCombatEndAndAdvanceTurn()
    {
        if(party.All(p => p.CurrentHP <= 0) || enemies.All(e => e.CurrentHP <= 0))
        {
            SetState(CombatState.CombatEnd);
            return true;
        }
        return false;
    }
    private IEnumerator ShowResultWithDelay(bool won)
    {
        yield return new WaitForSeconds(1.5f);
        UIController.Instance.ShowBattleResult(won);
    }
    private CharacterView FindView(CharacterStats stats)
    {
        return FindObjectsByType<CharacterView>(FindObjectsSortMode.None).FirstOrDefault(v => v.stats == stats);
    }
}