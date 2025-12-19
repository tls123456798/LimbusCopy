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
        uiController = FindFirstObjectByType<UIController>();
        if (uiController == null)
        {
            Debug.LogError("UIController를 찾을 수 없습니다.");
            return;
        }

        InitializeDummyData();
        SetState(CombatState.Setup);
    }

    private void InitializeDummyData()
    {
        // CharacterStats 생성자 매개변수 확인 필요 (현재 코드 기준 유지)
        CharacterStats player = new CharacterStats("Player", 100, 15, 10, 10);
        CharacterStats enemy = new CharacterStats("Enemy", 80, 15, 8, 8);

        // Skill 생성자 (id, name, scope, basePower, coinPower, maxCoinCount, cd)
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
                    // 적 AI 행동 결정
                    if (enemies.Contains(currentActor))
                    {
                        selectedTarget = party.FirstOrDefault(p => p.CurrentHP > 0);
                        selectedSkill = currentActor.AvailableSkills.FirstOrDefault();
                    }

                    if (selectedTarget != null)
                        targetClashSkill = selectedTarget.AvailableSkills.FirstOrDefault();

                    // 1. 데이터 리셋
                    if (selectedSkill != null) selectedSkill.ResetCoinCount();
                    if (targetClashSkill != null) targetClashSkill.ResetCoinCount();

                    // 2. UI 코인 생성 (플레이어가 왼쪽, 적이 오른쪽)
                    if (party.Contains(currentActor))
                        uiController.InitializeClashUI(selectedSkill.MaxCoinCount, targetClashSkill.MaxCoinCount);
                    else
                        uiController.InitializeClashUI(targetClashSkill.MaxCoinCount, selectedSkill.MaxCoinCount);

                    SetState(CombatState.ClashCalculation);
                    break;
                }

            case CombatState.ClashCalculation:
                {
                    yield return StartCoroutine(ClashCoinLoopRoutine(currentActor, selectedSkill, selectedTarget, targetClashSkill));

                    // [연출 추가] 합 결과가 끝나면 잠시 대기 후 데미지 계산으로 이동
                    yield return new WaitForSeconds(0.5f);
                    SetState(CombatState.CalculatingCombat);
                    break;
                }

            case CombatState.CalculatingCombat:
                {
                    // [연출 추가] 데미지 계산 시점에 합 UI(코인) 숨기기
                    uiController.HideClashUI();

                    if (selectedSkill.CurrentCoinCount > 0)
                    {
                        int dmg = CalculateFinalDamage(currentActor, selectedSkill);
                        selectedTarget.TakeDamage(dmg);
                        UpdateCombatVisuals(selectedTarget, currentActor, dmg);
                        SetState(CombatState.PlayingAnimation);
                    }
                    else if (targetClashSkill.CurrentCoinCount > 0)
                    {
                        int dmg = CalculateFinalDamage(selectedTarget, targetClashSkill);
                        currentActor.TakeDamage(dmg);
                        UpdateCombatVisuals(currentActor, selectedTarget, dmg);
                        SetState(CombatState.PlayingAnimation);
                    }
                    else
                    {
                        // 무승부 혹은 코인 모두 파괴 시 바로 다음 턴
                        OnAnimationFinished();
                    }
                    break;
                }

            case CombatState.PlayingAnimation:
                {
                    CharacterStats winner = selectedSkill.CurrentCoinCount > 0 ? currentActor : selectedTarget;
                    CharacterView view = FindView(winner);

                    if (view != null) view.PlayAttackAnimation();
                    else OnAnimationFinished();
                    break;
                }

            case CombatState.CombatEnd:
                {
                    bool playerWon = enemies.All(e => e.CurrentHP <= 0);
                    StartCoroutine(ShowResultWithDelay(playerWon));
                    break;
                }
        }
    }

    private IEnumerator ClashCoinLoopRoutine(CharacterStats aStats, Skill aSkill, CharacterStats dStats, Skill dSkill)
    {
        // 림버스 컴퍼니 방식: 위력이 낮은 쪽의 코인이 0이 될 때까지 반복
        while (aSkill.CurrentCoinCount > 0 && dSkill.CurrentCoinCount > 0)
        {
            var aRes = aSkill.GetExecutionResult();
            var dRes = dSkill.GetExecutionResult();

            // 1. 텍스트 정보 업데이트
            uiController.ShowClashResult(aStats.Name, aRes.finalPower, dStats.Name, dRes.finalPower, aSkill.CurrentCoinCount, dSkill.CurrentCoinCount);

            // 2. 코인 상태 시각화 (개수는 그대로, 앞/뒷면 결과만 반영)
            if (party.Contains(aStats))
                uiController.UpdateClashCoins(aSkill.CurrentCoinCount, aRes.headsCount, dSkill.CurrentCoinCount, dRes.headsCount);
            else
                uiController.UpdateClashCoins(dSkill.CurrentCoinCount, dRes.headsCount, aSkill.CurrentCoinCount, aRes.headsCount);

            yield return new WaitForSeconds(clashDisplayDuration);

            // 3. 위력 비교 및 패배한 쪽의 코인 개수 감소 (파괴 연출)
            if (aRes.finalPower > dRes.finalPower)
            {
                dSkill.CurrentCoinCount--;
                Debug.Log($"{dStats.Name}의 코인 파괴! 남은 코인: {dSkill.CurrentCoinCount}");
            }
            else if (dRes.finalPower > aRes.finalPower)
            {
                aSkill.CurrentCoinCount--;
                Debug.Log($"{aStats.Name}의 코인 파괴! 남은 코인: {aSkill.CurrentCoinCount}");
            }
            // 무승부 시에는 아무 일도 일어나지 않고 재투구

            // 4. 파괴된 결과를 UI에 즉시 반영 (코인이 사라지는 연출)
            if (party.Contains(aStats))
                uiController.UpdateClashCoins(aSkill.CurrentCoinCount, aRes.headsCount, dSkill.CurrentCoinCount, dRes.headsCount);
            else
                uiController.UpdateClashCoins(dSkill.CurrentCoinCount, dRes.headsCount, aSkill.CurrentCoinCount, aRes.headsCount);

            // 코인이 파괴된 후 잠깐 대기 (파괴되는 것을 눈으로 확인하기 위함)
            yield return new WaitForSeconds(0.3f);
        }
    }

    private int CalculateFinalDamage(CharacterStats attacker, Skill skill)
    {
        var result = skill.GetExecutionResult();
        // 공격력 + 최종 위력 (림버스 스타일 데미지 공식 예시)
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

    private IEnumerator ShowResultWithDelay(bool won)
    {
        yield return new WaitForSeconds(1.5f);
        uiController.ShowBattleResult(won);
    }

    private CharacterView FindView(CharacterStats stats)
    {
        return FindObjectsByType<CharacterView>(FindObjectsSortMode.None).FirstOrDefault(v => v.stats == stats);
    }
}