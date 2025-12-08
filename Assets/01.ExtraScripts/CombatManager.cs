// CombatManager.cs
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class CombatManager : MonoBehaviour
{
    // 현재 전투의 상태
    private CombatState currentState = CombatState.Setup;

    // 현재 행동할 캐릭터 (Queue에서 꺼내온 캐릭터)
    private CharacterStats currentActor;

    // 모든 아군 및 적 캐릭터 목록
    public List<CharacterStats> party = new List<CharacterStats>();
    public List<CharacterStats> enemies = new List<CharacterStats>();

    // 턴 순서를 관리하는 대기열
    private Queue<CharacterStats> turnOrderQueue = new Queue<CharacterStats>();

    // UI 조작을 위한 플래그 (임시)
    private bool isInputReceived = false;

    // ----- 초기 설정 -----
    void Start()
    {
        // 예시로 캐릭터 생성 (실제로는 로드 필요)
        party.Add(new CharacterStats("Man", 100, 15, 10));
        enemies.Add(new CharacterStats("Monster", 80, 20, 8));

        // 초기 상태 시작
        SetState(CombatState.Setup);
    }

    // ----- 핵심: 상태 변경 메서드 -----
    // 상태를 변경할 때마다 필요한 초기화 작업을 수행합니다.
    public void SetState(CombatState newState)
    {
        // 상태 전/후 디버깅
        Debug.Log($"전투 상태 변경: {currentState} -> {newState}");
        currentState = newState;

        // 각 상태에 따른 초기 액션 수행
        switch (currentState)
        {
            case CombatState.CalculatingCombat:
                int totalDamage = 15;
                SetState(CombatState.PlayingAnimation); // 초기화 후 바로 턴 시작
                break;

            case CombatState.StartTurn:
                PrepareTurnOrder(); // 턴 순서 정렬
                currentActor = turnOrderQueue.Dequeue(); // 첫 번째 행동할 캐릭터 가져오기

                // UIController에게 "누가 턴 시작했음" 알림 (화면 업데이트)
                // UIController.Instance.DisplayTurnInfo(currentActor);

                // 아군 턴이면 입력 대기 상태로, 적 턴이면 계산/애니메이션 상태로 이동
                if (party.Contains(currentActor))
                {
                    SetState(CombatState.WaitingForInput);
                }
                else
                {
                    SetState(CombatState.CalculatingCombat); // 적의 AI는 바로 계산으로 넘어감
                }
                break;

            case CombatState.WaitingForInput:
                // UIController에게 "스킬 선택 창을 띄워라" 명령
                // UIController.Instance.ShowSkillSelection(currentActor.Skills);
                isInputReceived = false; // 입력 대기 시작
                break;

            // ... 나머지 상태들 ...

            case CombatState.CombatEnd:
                Debug.Log("전투 종료!");
                // ... 결과 화면 표시 ...
                break;
        }
    }

    // ----- 턴 순서 정렬 (Speed 기준) -----
    private void PrepareTurnOrder()
    {
        // 모든 캐릭터를 합치고 Speed 기준으로 내림차순 정렬
        List<CharacterStats> allUnits = new List<CharacterStats>(party.Concat(enemies));
        allUnits = allUnits.OrderByDescending(unit => unit.Speed).ToList();

        // Queue에 넣기
        turnOrderQueue.Clear();
        foreach (var unit in allUnits)
        {
            if (unit.CurrentHP > 0)
            {
                turnOrderQueue.Enqueue(unit);
            }
        }
    }

    // ----- 핵심: 유니티 Update 루프를 통한 상태 유지/전환 -----
    void Update()
    {
        // 매 프레임마다 현재 상태를 확인하고 필요한 작업을 수행합니다.
        switch (currentState)
        {
            case CombatState.WaitingForInput:
                // 유저 입력이 들어왔는지 계속 체크
                if (isInputReceived)
                {
                    // 입력이 들어왔으면, 다음 단계인 계산 상태로 전환
                    SetState(CombatState.CalculatingCombat);
                }
                break;

            case CombatState.PlayingAnimation:
                // 캐릭터 애니메이션 재생이 끝났는지 체크
                // if (CharacterView.IsAnimationFinished())
                // {
                //     // 애니메이션이 끝나면, 다음 턴을 시작하거나 전투 종료를 체크
                //     CheckCombatEnd();
                // }
                break;
        }
    }

    // ----- 외부에서 호출되는 메서드 (UI 이벤트) -----
    // 유저가 스킬 선택 버튼을 눌렀을 때 UIController가 이 메서드를 호출합니다.
    public void OnSkillSelected(string selectedSkill, CharacterStats target)
    {
        if (currentState == CombatState.WaitingForInput)
        {
            // 선택된 스킬/타겟 정보를 저장
            // this.selectedSkill = selectedSkill;
            // this.target = target;

            isInputReceived = true; // 입력 완료 플래그를 True로 설정하여 Update에서 상태 전환 유도
        }
    }

    // ----- 턴 종료 체크 및 다음 턴 준비 -----
    private void CheckCombatEnd()
    {
        // 승리/패배 조건 체크 (예: 적 HP가 모두 0인지)
        if (enemies.All(e => e.CurrentHP <= 0))
        {
            SetState(CombatState.CombatEnd);
        }
        else if (turnOrderQueue.Count > 0)
        {
            // 턴이 남아있으면 다음 캐릭터에게 넘김
            SetState(CombatState.StartTurn);
        }
        else
        {
            // Queue가 비었으면, 다음 라운드를 위해 턴 순서를 다시 정렬하고 StartTurn으로 이동
            SetState(CombatState.StartTurn);
        }
    }
}