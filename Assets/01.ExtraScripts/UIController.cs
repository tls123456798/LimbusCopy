using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class UIController : MonoBehaviour
{
    // 싱글턴 패턴
    public static UIController Instance { get; private set; }

    // UI 요서 연결
    [Header("UI 요소")]
    public GameObject skillPanel; // 스킬 선택 패널
    public Text turnInfoText; // 현재 턴 정보 출력 텍스트

    [Header("동적 스킬 UI 요소")]
    // Inspector에서 연결 필수
    public GameObject skillButtonPrefab; // 스킬 버튼 프리팹
    public Transform skillButtonParent; // 동적 버튼 생성될 부모

    [Header("타겟팅")]
    public LayerMask targetLayer; // 캐릭터가 포함된 레이어

    // 상태 변수
    private CombatManager combatManager;
    private bool isPlayerTurn = false;

    private CharacterStats selectedTarget; // 마우스로 선택된 타겟
    private Skill selectedSkill; // 선택된 Skill 객체
    private bool isSelectingTarget;

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
    /// 플레이어 턴일 때 스킬 선택 UI를 보여줍니다.
    /// </summary>
    /// <param name="availableSkills">사용 가능한 스킬 목록 (여기서는 사용하지 않고 버튼만 활성화)</param>
    public void ShowSkillSelection(CharacterStats actor)
    {
        isPlayerTurn = combatManager.party.Contains(actor);
        if (!isPlayerTurn) return; // 플레이어 턴이 아니면 처리하지 않음

        if (turnInfoText != null)
        {
            turnInfoText.text = $"현재 턴: {actor.Id} (스킬 선택)";
        }
        // 이전 생성된 버튼 모두 제거 (클리어)
        if (skillButtonParent != null)
        {
            foreach (Transform child in skillButtonParent)
            {
                Destroy(child.gameObject);
            }
        }
        // 스킬 목록이 없거나 Parent 설정이 안 되있으면 패널 숨김
        if (actor.AvailableSkills == null || actor.AvailableSkills.Count == 0 || skillButtonParent == null)
        {
            skillPanel.SetActive(false);
            return;
        }
        // 스킬 목록을 순회하며 버튼 동적 생성
        foreach (var skill in actor.AvailableSkills)
        {
            // 쿨타임이 0이 아닌 스클은 스킵 (비활성화 상태로 만들거나 continue)
            if (actor.SkillCooldowns.ContainsKey(skill.Id) && actor.SkillCooldowns[skill.Id] > 0)
            {
                continue;
            }
            // 버튼 생성 및 설정
            GameObject buttonObj = Instantiate(skillButtonPrefab, skillButtonParent);
            Button buttonComp = buttonObj.GetComponent<Button>();

            // 텍스트 컴포넌트 안전 호출 및 설정
            Text buttonText = buttonObj.GetComponentInChildren<Text>();
            if (buttonText != null)
            {
                buttonText.text = skill.Name;
            }
            else
            {
                // 2. TextMeshProUGUI 컴포넌트를 사용하는 경우
                // 🚨 UnityEngine.UI가 아닌 TMPro 네임스페이스를 사용해야 합니다.
                TMPro.TextMeshProUGUI tmproText = buttonObj.GetComponentInChildren<TMPro.TextMeshProUGUI>();

                if (tmproText != null)
                {
                    tmproText.text = skill.Name;
                }
                else
                {
                    Debug.LogError($"경고: 스킬 버튼 프리팹 '{skillButtonPrefab.name}'에 Text 또는 TextMeshProUGUI 컴포넌트가 없습니다.");
                }
            }
            // 버튼 클릭 리스너 등록: 타겟 선택 모드로 전환하며 Skill 객체 전달
            // 람다식 내에서 skill 변수 캡처를 통해 정확한 Skill 객체를 전달합니다.
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

        selectedSkill = skill; //Skill 객체 저장

        // 스킬 범위에 따른 즉시 처리
        if (selectedSkill.Scope == TargetScope.Self)
        {
            // 자신을 타겟으로 즉시 지정하고 전투 계산 요청
            selectedTarget = combatManager.currentActor;
            isSelectingTarget = false;
            skillPanel.SetActive(false);
            combatManager.OnSkillSelected(selectedSkill, selectedTarget);
        }
        else if (selectedSkill.Scope == TargetScope.AllEnemies)
        {
            // (TODO: AllEnemies 처리 로직 추가 필요. 현재는 단일 타겟팅만 가정하고 진행)
            // 임시로 그냥 전투 계산으로 넘려 CombatManager가 모든 적을 처리하도록 할 수 있음
            isSelectingTarget = false;
            skillPanel.SetActive(false);
            combatManager.OnSkillSelected(selectedSkill, null); // 타겟이 null이면 전체 공격 으로 처리하도록 CombatManager에서 구현 필요
        }
        else // SingleEnemy, SingleAlly 등 타겟 지정이 필요한 경우
        {
            isSelectingTarget = true;
            if (turnInfoText != null)
            {
                turnInfoText.text = $"타겟 선택 중...({skill.Name})";
            }
        }
    }

    /// <summary>
    /// 타겟 선택 모드일 때 마우스 입력을 처리합니다.
    /// </summary>
    private void HandleTargetInput()
    {
        // 마우스 왼쪽 버튼 클릭 시 타겟 확정
        if(Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            // Target Layer에 대해서만 Raycast 수행
            if(Physics.Raycast(ray, out hit, 100f, targetLayer))
            {
                CharacterView targetView = hit.collider.GetComponent<CharacterView>();

                if(targetView != null && targetView.stats != null)
                {
                    // 현재는 '단일 적' 타겟팅만 허용한다고 가정 (TargetScope.SingleEnemy)
                    if(selectedSkill.Scope == TargetScope.SingleEnemy && combatManager.enemies.Contains(targetView.stats))
                    {
                        ConfirmTarget(targetView.stats);
                    }
                    else if(selectedSkill.Scope == TargetScope.SingleAlly && combatManager.party.Contains(targetView.stats))
                    {
                        ConfirmTarget(targetView.stats);
                    }
                    else
                    {
                        // 유효하지 않는 타겟 타입
                        Debug.Log("선택된 타겟은 현재 스킬의 유효 범위가 아닙니다.");
                    }
                }
            }
        }
    }
    private void ConfirmTarget(CharacterStats target)
    {
        selectedTarget =target;

        // 입력 완료 및 CombatManager에게 Skill 객체와 타겟 전달
        isSelectingTarget = false;
        skillPanel.SetActive(false);

        if(turnInfoText != null)
        {
            turnInfoText.text = $"타겟 확정: {selectedTarget.Id}로 {selectedSkill.Name} 사용";
        }

        // 핵심 호출: CombatManager의 상태를 CalculatingCombat으로 전환
        combatManager.OnSkillSelected(selectedSkill,selectedTarget);
    }
}
  