using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class UIController : MonoBehaviour
{
    [Header("동적 스킬 UI 요소")]
    public GameObject skillButtonPrefab; // 스킬 버튼 프리팹
    public Transform skillButtonParent; // 스킬 버튼이 생성될 부모 Transform(Panel)

    // 싱글턴 패턴 (옵션: Manager 외에 UI도 싱글턴으로 접근할 경우)
    public static UIController Instance { get; private set; }

    // UI 요소 연결 (Unity Inspector에서 연결)
    [Header("UI 요소")]
    public GameObject skillPanel; // 스킬 선택 패널 (활성화/비활성화용)
    public Button attackButton; // 기본 공격 버튼 (가정)
    public TMP_Text turnInfoText; // 현재 턴 정보 출력 텍스트

    [Header("타겟팅")]
    public LayerMask targetLayer; // 캐릭터가 포함된 레이어 (Raycast용)
    private CharacterStats selectedTarget; // 현재 마우스로 선택된 타겟

    // 상태 변수
    private CombatManager combatManager;
    private bool isPlayerTurn = false;
    private bool isSelectingTarget = false;

    // 초기화 및 Unity LifeCycle
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
    private void Start()
    {
        combatManager = CombatManager.Instance;
        if (combatManager == null)
        {
            Debug.LogError("CombatManager를 찾을 수 없습니다.");
            return;
        }

        // 초기 설정: UI 숨기기
        skillPanel.SetActive(false);
        // TurnInfoText 초기화 (선택 사항)
        if (turnInfoText != null)
        {
            turnInfoText.text = "전투 대기 중...";
        }
    }
    private void Update()
    {
        // 타겟 선택 모드일 때 마우스 입력 처리
        if (isSelectingTarget)
        {
            HandleTargetInput();
        }
    }
    // CombatManager 에서 호출되는 메서드

    /// <summary>
    /// CombatManager 가 턴 정보를 표시하라고 요청할 때 호출됩니다.
    /// </summary>
    public void DisPlayturnInfo(CharacterStats actor)
    {
        turnInfoText.text = $"현재 턴: {actor.Id}";
        isPlayerTurn = combatManager.party.Contains(actor);
        if (isPlayerTurn)
        {
            // 플레이어 턴일 때만 스킬/공격 UI 활성화
            ShowSkillSelection(actor.Skills); // actor.Skills는 CharacterStats에 정의되어 있다고 가정
        }
        else
        {
            skillPanel.SetActive(false);
        }
    }

    private void ShowSkillSelection(List<string> skills)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// 플레이어 턴일 때 스킬 선택 UI를 보여줍니다.
    /// </summary>
    /// <param name="availableSkills">사용 가능한 스킬 목록 (여기서는 사용하지 않고 버튼만 활성화)</param>
    public void ShowSkillSelection(CharacterStats actor)
    {
        // 이전 생성된 버튼 모두 제거
        foreach (Transform child in skillButtonParent)
        {
            Destroy(child.gameObject);
        }
        if (actor.AvailableSkills == null || actor.AvailableSkills.Count == 0)
        {
            skillPanel.SetActive(false);
            return;
        }
        // 스킬 목록을 순회하며 버튼 동적 생성
        foreach (var skill in actor.AvailableSkills)
        {
            // 쿨타임이 0이 아닌 스킬은 스킴 (쿨타임 로직은 EndTurnCleanup에서 관리됨)
            //if(actor.SkillCooldowns.ContainsKey(skill.Id) && actor.SkillCooldowns[skill.Id] > 0)
            //continue;

            // 버튼 생성
            GameObject buttonObj = Instantiate(skillButtonPrefab, skillButtonParent);
            Button buttonComp = buttonObj.GetComponent<Button>();
            // 버튼 텍스트 설정
            buttonObj.GetComponentInChildren<Text>().text = skill.Name;

            // 버튼 클릭 시 타겟 선택 모드로 저환하면서 Skill 객체 자체를 전달
            buttonComp.onClick.AddListener(() => StartTargetSelection(skill));
        }

        skillPanel.SetActive(true);
        isSelectingTarget = false;
        selectedTarget = null;
    }
    // UI 입력 처리 로직

    /// <summary>
    /// 스킬 버튼 클릭 시 타겟 서택 모드를 시작합니다.
    /// </summary>
    /// <param name="skillId">선택된 스킬 ID</param>
    public void StartTargetSelection(Skill skill)
    {
        if (!isPlayerTurn) return;

        //selectedSkill = skill;
        isSelectingTarget = true;
        turnInfoText.text = $"타겟 선택 중...({skill.Name})";
        // 선택된 스킬 ID를 임시로 저장해 두었다가 입력 완료 시 CombatManager로 전달
        // (이 예제에서는 attackButton 리스너에 직접 "BasicAttack"을 전달
    }
    /// <summary>
    /// 타겟 선택 모드일 때 마우스 입력을 처리합니다.
    /// </summary>
    private void HandleTargetInput()
    {
        // 마우스로 타겟 하이라이트 (시각적 피드백 로직)
        // Raycast를 사용하여 마우스 위치의 캐릭터를 확인하고 하이라이트할 수 있습니다.

        // 마우스 왼쪽 버튼 클릭 시 타겟 확정
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            // CharacterView 또는 CharacterStats 컴포넌트가 붙은 오브젝트를 타겟팅
            if (Physics.Raycast(ray, out hit, 100f, targetLayer))
            {
                CharacterView targetView = hit.collider.GetComponent<CharacterView>();
                if (targetView != null)
                {
                    // 타겟이 combatManager의 적 리스트에 포함되는지 확인
                    // 공격 대상은 적만 가능하다고 가정
                    if (combatManager.enemies.Contains(targetView.stats))
                    {
                        selectedTarget = targetView.stats;

                        // 입력 완료 및 CombatManager 에게 전달
                        isSelectingTarget = false;
                        skillPanel.SetActive(false);
                        turnInfoText.text = $"타겟 확정: {selectedTarget.Id}";

                        // CombatManager에게 스킬 ID와 타겟 전달 (여기서는 "BasicAttack" 가정)
                        //combatManager.OnSkillSelected(selectedSkill, selectedTarget);
                    }
                }
            }
        }
    }
}