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
        currentActor = actor;

        // 필수 UI 필드 존재 여부 확인 (NRE 방지)
        if(skillPanel == null || skillButtonPrefab == null || skillButtonParent == null)
        {
            Debug.LogError("UIController의 필수 UI 필드 연결이 누락되었습니다. 상태 전환 실패");
            return;
        }
        // 턴 정보 업데이트
        if(turnInfoText != null)
        {
            turnInfoText.text = $"{actor.Name}의 턴입니다.";
        }
        // 스킬 목록 확인 (스킬 데이터 누락 방지)
        if(actor.AvailableSkills == null || actor.AvailableSkills.Count == 0)
        {
            Debug.LogWarning($"{actor.Name}에게 사용할 수 없어 스킬 패널을 표시하지 않습니다.");
            skillPanel.SetActive(false);
            return;
        }
        foreach(var  skill in actor.AvailableSkills)
        {
            // 인스턴스호 시 skillButtonParent를 부모로 설정
            GameObject buttonObj = Instantiate(skillButtonPrefab, skillButtonParent);
            Button buttonComp = buttonObj.GetComponent<Button>();

            // 버튼 텍스트 설정 (NRE 방지용 TMPro 널 체크 포함)
            TextMeshProUGUI buttonText = buttonObj.GetComponentInChildren<TextMeshProUGUI>();

            if(buttonText != null)
            {
                buttonText.text = skill.Name;
            }
            else
            {
                Debug.LogError($"'{skillButtonPrefab.name}' 프리팹 내부에 TextMeshProUGUI 컴폰너트가 없어 스킬 이름 설정 불가.");
            }
            // 버튼 클릭 리스너 등록
            buttonComp.onClick.AddListener(() => StartTargetSelection(skill));
        }
        // 스킬 선택 패널 활성화
        skillPanel.SetActive(true);

        // 상태 전환 요청 (WaitingForInput 으로 진입)
        // 이 코드가 성공적으로 실행되어야 CombatManager가 다음 상태로 넘어갑니다.
        CombatManager.Instance.SetState(CombatState.WaitingForInput);
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
                CharacterView targetView = hit.collider.GetComponentInParent<CharacterView>();

                if (targetView != null)
                {
                    targetView = hit.collider.GetComponent<CharacterView>();
                }
                if(targetView != null)
                {
                    CharacterStats target = targetView.stats;
                    if(target != null)
                    {
                        EndTargetSelection(target);
                    }
                }
                else
                {
                    Debug.LogWarning($"레이캐스트가 {hit.collider.name}에 맞았지만, CharacterStats 컴포넌트를 찾을 수 없습니다.");
                }
            }
        }
    }

    private void EndTargetSelection(CharacterStats target)
    {
        isSelectingTarget = false;
        // 선택된 타겟을 CcombatManager 가 참조할 수 있도록 설정
        CombatManager.Instance.selectedTarget = target;
        CombatManager.Instance.selectedSkill = selectedSkill;

        // CombatManager에게 계산을 시작하도록 요청
        CombatManager.Instance.SetState(CombatState.CalculatingCombat);
    }
}