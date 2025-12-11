using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using Unity.VisualScripting.Antlr3.Runtime.Misc;
public class CombatManager : MonoBehaviour
{
    // 3. 핵심 변수 (싱글톤 및 데이터)

    // 싱글톤 인스턴스
    public static CombatManager Instance { get; private set; }

    [Header("전투 데이터")]
    public List<CharacterStats> party = new List<CharacterStats>();
    public List<CharacterStats> enemies = new List<CharacterStats>();

    // 데이터와 View를 연결하는 맵
    private Dictionary<string, CharacterView> characterViewMap = new Dictionary<string, CharacterView>();

    [Header("현재 상태 및 흐름")]
    private CombatState currentState = CombatState.Setup;
    private Queue<CharacterStats> turnOrderQueue = new Queue<CharacterStats>();

    // 턴 정보
    private CharacterStats currentActor;
    private CharacterStats selectedTarget; // 유저 입력 시 할당됨
    private Skill selectedSkill;
    // private string selectedSkillId; // 유저 입력 시 할당됨

    // UI 플래그
    private bool isInputReceived = false;
    private int damageAfterResistance;

    // 4. 유니티 라이프사이클 및 싱글톤 초기화
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            Instance = this;
        }
    }
    private void Start()
    {
        // 임시 캐릭터 생성
        party.Add(new CharacterStats("Man", 100, 15, 10, 10)); // ID, HP, ATK, SPD, DEF
        enemies.Add(new CharacterStats("Monster", 80, 20, 8, 8)); // ID, HP, ATK, SPD, DEF

        SetState(CombatState.Setup);
    }
    // 5. State Machine 핵심 메서드

    public void SetState(CombatState newState)
    {
        Debug.Log($"전투 상태 변경: {currentState} -> {newState}");
        currentState = newState;

        switch (currentState)
        {
            case CombatState.Setup:
                PrepareTurnOrder();
                SetState(CombatState.StartTurn);
                break;

            case CombatState.StartTurn:
                //UIController.Instance.ShowSkillSelection(new List<string>());
                SetState(CombatState.WaitingForInput);
                if (turnOrderQueue.Count == 0)
                {
                    PrepareTurnOrder();
                }
                if (party.Contains(currentActor))
                {
                    // UIController.Instance.ShowSkillSelection(currentActor.Skills); // UI 활성화
                    isInputReceived = false;

                    // 다음 상태를 WaitingForInput으로 설정
                    SetState(CombatState.WaitingForInput);
                    // 여기에 break;를 추가하거나, switch 문을 빠져나가도록 해야 함
                    return; // SetState를 호출했으므로 함수를 종료

                    //UIController.Instance.DisPlayturnInfo(currentActor);

                    //isInputReceived = false;
                    //SetState(CombatState.WaitingForInput);   
                }
                // 다음 행동할 캐릭터 할당 및 사망자 스킵 처리
                currentActor = turnOrderQueue.Dequeue();
                while (currentActor.CurrentHP <= 0 && turnOrderQueue.Count > 0)
                {
                    currentActor = turnOrderQueue.Dequeue();
                }
                if (currentActor.CurrentHP <= 0 && turnOrderQueue.Count == 0)
                {
                    CheckCombatEndAndAdvanceTurn();
                    return;
                }
                if (party.Contains(currentActor))
                {
                    isInputReceived = false;
                    SetState(CombatState.WaitingForInput);
                }
                else
                {
                    // 적 AI 턴: 임시 타겟 설정 후 바로 계산
                    selectedTarget = party.FirstOrDefault(p => p.CurrentHP > 0);
                    selectedSkill = "EnemyAttack";
                    SetState(CombatState.CalculatingCombat);
                }
                break;
            case CombatState.WaitingForInput:
                // UIController.Instance.ShowSkillSelection(currentActor.Skills);
                break;
            case CombatState.CalculatingCombat:
                if (selectedTarget == null || selectedTarget.CurrentHP <= 0)
                {
                    Debug.LogWarning("타겟이 유효하지 않아 다음 턴으로 넘깁니다.");
                    SetState(CombatState.PlayingAnimation);
                    break;
                }
                int totalDamage = CalculateLimbusDamage(currentActor, selectedTarget);

                // 3. 계산된 데미지를 타겟 캐릭터 데이터에 반영
                selectedTarget.TakeDamage(totalDamage);
                // 시각적 요소에게 결과를 알미 및 업데이트
                CharacterView targetView = FindView(selectedTarget);
                if (targetView != null)
                {
                    targetView.UpdateHealthBar();
                }

                // 5. 애니메이션 상태로 전환
                SetState(CombatState.PlayingAnimation);
                break;

            case CombatState.PlayingAnimation:
                // CharacterView에게 애니메이션 재생을 명령하고 콜백을 기다림
                CharacterView actorview = FindView(currentActor);
                if (actorview != null)
                {
                    actorview.PlayAttackAnimation("Attack");
                }
                else
                {
                    // View 가 없으면 애니메이션 없이 바로 다음 턴으로 전환
                    OnAnimationFinished();
                }
                break;

            case CombatState.CombatEnd:
                Debug.Log("전투 종료!");
                // UIController.Instance.ShowResultScreen();
                break;
        }
    }
    // 외부 콜백 및 턴 흐름 제어

    // 유저 입력 (UIController에서 호출됨)
    public void OnSkillSelected(Skill skill, CharacterStats target)
    {
        if (currentState == CombatState.WaitingForInput)
        {
            this.selectedTarget = target;
            this.selectedSkill = skill;
            SetState(CombatState.CalculatingCombat);
        }
        else
        {
            Debug.LogWarning($"OnSkillSelected 호출 시 상태 오류: 현재 상태 {currentState}");
        }
    }
    public void OnAnimationFinished()
    {
        if (currentState == CombatState.PlayingAnimation)
        {
            EndTurnCleanup(); // 턴 정리 작업 수행
            CheckCombatEndAndAdvanceTurn();
        }
    }
    // 턴 순서 정렬 (Speed 기준)
    private void PrepareTurnOrder()
    {
        List<CharacterStats> allUnits = party.Concat(enemies).Where(c => c.CurrentHP > 0).ToList();
        allUnits = allUnits.OrderByDescending(Unit => Unit.Speed).ToList();

        turnOrderQueue.Clear();
        foreach (var unit in allUnits)
        {
            turnOrderQueue.Enqueue(unit);
        }
    }
    // 승리/패배 체크 및 다음 턴 진행
    private void CheckCombatEndAndAdvanceTurn()
    {
        if (enemies.TrueForAll(e => e.CurrentHP <= 0))
        {
            SetState(CombatState.CombatEnd);
            return;
        }
        if (party.TrueForAll(p => p.CurrentHP <= 0))
        {
            SetState(CombatState.CombatEnd);
            return;
        }
        if (turnOrderQueue.Count == 0 && party.Any(p => p.CurrentHP > 0) && enemies.Any(e => e.CurrentHP > 0))
        {
            PrepareTurnOrder();
        }
        SetState(CombatState.StartTurn);
    }
    // 데이터/뷰 연결 및 정리 로직

    // 뷰 등록 (Scene 초기화 시 호출 필요)
    public void RegisterView(CharacterView view)
    {
        if (!characterViewMap.ContainsKey(view.stats.Id))
        {
            characterViewMap.Add(view.stats.Id, view);
        }
    }
    private CharacterView FindView(CharacterStats stats)
    {
        if (characterViewMap.TryGetValue(stats.Id, out CharacterView view))
        {
            return view;
        }
        return null;
    }
    // 턴 종료 정리 (쿨타임, 상태 이상 감소)
    private void EndTurnCleanup()
    {
        List<CharacterStats> allCharacters = party.Concat(enemies).Where(c => c.CurrentHP > 0).ToList();
    }
    private int CalculateLimbusPower(int basePower, int coinCount, int coinBonus)
    {
        int finalPower = basePower;
        for (int i = 0; i < coinCount; i++)
        {
            if (Random.Range(0, 2) == 1) // 50% 성공률 가정
            {
                finalPower += coinBonus;
            }
        }
        return finalPower;
    }
    private int CalculateLimbusDamage(CharacterStats attacker, CharacterStats defender)
    {
        // 고정 값 대신 selectedskill의 데이터를 사용
        if (selectedSkill == null)
        {
            Debug.LogError("선택된 스킬 데이터가 업습니다. 기본 공격을 가정합니다.");
            // 임시 기본 공격 값 사용 (없을 경우를 대비)
            return CalculateLimbusDamageFallback(5, 2, 3, defender);
        }
        // 공격력 계산
        int attackPower = CalculateLimbusPower(
            selectedSkill.BasePower,
            selectedSkill.CoinCount,
            selectedSkill.CoinBonus
            );

        // 기본 데미지 계산
        int baseDamage = attackPower - defender.Defender;
        int damgeAfterResistance = baseDamage;

        // 스킬 효과 (상태 이상) 적용 로직 추가
        ApplySkillEffects(selectedSkill, defender);

        return Mathf.Max(1, damageAfterResistance);
    }
    private int CalculateLimbusDamageFallback(int baseP, int coinC, int coinB, CharacterStats defender)
    {
        int power = CalculateLimbusPower(baseP, coinC, coinB);
        return Mathf.Max(1, power - defender.Defender);
    }

    internal void OnSkillSelected(string v, CharacterStats selectedTarget)
    {
        throw new System.NotImplementedException();
    }
    private void ApplySkillEffects(Skill skill, CharacterStats target)
    {
        if (skill.EffectsToApply != null)
        {
            foreach (var effect in skill.EffectsToApply)
            {
                // 기존 효과가 있는지 확인하고 Duration을 갱신하거나, 새 효과를 추가
                // 간단화를 위해 여기서는 무조건 새로 추가한다고 가정
                // 실제 게임에서는 중복 효과 처릭 복잡하므로, Unique ID 등으로 관리해야 합니다.

            }
        }
    }
}