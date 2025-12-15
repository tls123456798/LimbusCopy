using UnityEngine;
using UnityEngine.UI; // Text (Legacy) 또는 Button 등을 위해 필요
using TMPro;         // TextMeshProUGUI를 위해 필요
using System.Collections.Generic;

public class UIController : MonoBehaviour
{
    // === 싱글톤 인스턴스 ===
    public static UIController Instance { get; private set; }

    // === UI 요소 연결 (Inspector에서 연결되어야 함) ===
    [Header("UI 요소")]
    public GameObject skillPanel;         // 스킬 버튼들을 담는 부모 GameObject (Skill Panel)
    public Text turnInfoText; // 턴 정보 출력 텍스트 (Turn Info Text)

    [Header("동적 스킬 UI 요소")]
    public GameObject skillButtonPrefab;  // 스킬 버튼 프리팹 (Skill Button Prefab)
    public Transform skillButtonParent;   // 버튼이 생성될 부모 Transform (Skill Button Parent)

    [Header("타겟팅")]
    public LayerMask targetLayer;       // 타겟팅 가능한 오브젝트 레이어 (Enemy)

    // === 내부 상태 ===
    private bool isSelectingTarget = false;
    private CharacterStats selectedTarget;
    private CharacterStats currentActor;
    private Skill selectedSkill;

    // === 초기화 ===
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // DontDestroyOnLoad(gameObject); // 필요에 따라 추가
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        // NullReferenceException 방지를 위해 안전 체크
        if (skillPanel == null)
        {
            Debug.LogError("UIController: Skill Panel이 Inspector에 연결되지 않았습니다.");
            return;
        }

        // 초기에는 UI를 숨깁니다.
        skillPanel.SetActive(false);
    }

    // === 턴 시작 시 호출 (CombatManager.SetState(StartTurn)에서 호출됨) ===
    public void ShowSkillSelection(CharacterStats actor)
    {
        // 현재 액터를 설정하고 턴 정보를 업데이트합니다.
        currentActor = actor;
        if (turnInfoText != null)
        {
            turnInfoText.text = $"{actor.Name}의 턴입니다.";
        }

        // 🚨 이전에 논의된 문제: 스킬 목록이 비어 있으면 WaitingForInput으로 넘어가지 못함
        if (actor.AvailableSkills == null || actor.AvailableSkills.Count == 0 || skillButtonParent == null || skillButtonPrefab == null)
        {
            Debug.LogWarning($"{actor.Name}에게 사용할 수 있는 스킬이 없거나 UI 컴포넌트가 누락되어 스킬 패널을 표시할 수 없습니다.");
            if (skillPanel != null)
            {
                skillPanel.SetActive(false);
            }
            return; // 🚨 스킬이 없으면 WaitingForInput으로 전환할 필요가 없으므로 여기서 종료
        }
        foreach (Transform child in skillButtonParent.transform)
        {
            {
                Destroy(child.gameObject);
            }

            // 스킬 목록을 순회하며 버튼 동적 생성
            foreach (var skill in actor.AvailableSkills)
            {
                GameObject buttonObj = Instantiate(skillButtonPrefab, skillButtonParent);
                Button buttonComp = buttonObj.GetComponent<Button>();

                // 🚨 80줄 부근 NRE 문제 해결을 위한 안전 코드 (이전 단계에서 논의됨)
                TextMeshProUGUI buttonText = buttonObj.GetComponentInChildren<TextMeshProUGUI>();
                if (buttonText != null)
                {
                    buttonText.text = skill.Name;
                }
                else
                {
                    // TMPro 컴포넌트가 없는 경우를 위한 로그
                    Debug.LogError($"'{skillButtonPrefab.name}' 프리팹 내부에 TextMeshProUGUI 컴포넌트가 없습니다.");
                }

                // 버튼 클릭 리스너 등록: 타겟 선택 모드로 전환하며 Skill 객체 전달
                buttonComp.onClick.AddListener(() => StartTargetSelection(skill));
            }

            // 스킬 선택 패널 활성화
            skillPanel.SetActive(true);

            // 🚨 CombatManager에게 상태를 WaitingForInput으로 바꾸도록 요청
            // 이 코드가 성공적으로 실행되어야 다음 상태로 전환됩니다.
            CombatManager.Instance.SetState(CombatState.WaitingForInput);
        }
    }

    // === 타겟 선택 로직 ===
    private void StartTargetSelection(Skill skill)
    {
        // 스킬 선택 UI 숨기기
        skillPanel.SetActive(false);

        // 타겟팅 상태 시작
        isSelectingTarget = true;
        selectedSkill = skill;

        Debug.Log($"타겟 선택 시작: {skill.Name}");
    }

    private void Update()
    {
        if (isSelectingTarget)
        {
            HandleTargetInput();
        }
    }

    private void HandleTargetInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, 100f, targetLayer))
            {
                // 타겟 오브젝트에서 CharacterStats 컴포넌트를 찾음
                CharacterStats target = hit.collider.GetComponentInParent<CharacterStats>();

                if (target != null)
                {
                    // 타겟팅 완료
                    EndTargetSelection(target);
                }
            }
        }
    }

    private void EndTargetSelection(CharacterStats target)
    {
        isSelectingTarget = false;
        selectedTarget = target;

        // CombatManager에게 계산을 시작하도록 요청
        CombatManager.Instance.SetState(CombatState.CalculatingCombat);
    }
}