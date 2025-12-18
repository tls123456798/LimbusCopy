using UnityEngine;
using UnityEngine.UI; // Text (Legacy) 또는 Button 등을 위해 필요
using TMPro;         // TextMeshProUGUI를 위해 필요
using System.Collections.Generic;
using UnityEngine.SceneManagement; // 씬 전환을 위해 추가

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

    [Header("전투 결과 표시")]
    public GameObject clashResultPanel; // 합 결과를 표시할 패널
    public TextMeshProUGUI clashResultText; // 합 결과 텍스트

    [Header("전투 종료 결과 UI")]
    public GameObject battleResultPanel; // 승리/패배 전체 패널
    public TextMeshProUGUI resultTitleText; // "VICTORY" 또는 "DEFEAT" 표시

    // 게임 재시작 및 타이틀 화면 으로 돌아가는 버튼
    public GameObject optionPanel; // 방금 만든 OptionPanel을 연결
    public Button restartButton; // 다시 시작 버튼
    public Button lobbyButton; // 로비로 이동 버튼

    // === 내부 상태 ===
    private bool isSelectingTarget = false;
    private CharacterStats currentActor;
    private Skill selectedSkill;

    [Header("코인 연출 요소")]
    public GameObject coinPrefab; // 코인 이미지 프리팹
    public Transform playerCoinParent; // 플레이어 코인이 배치될 부모(Layout Group)
    public Transform enemyCoinParent; // 적 코인이 배치될 부모

    // 코인들을 관리할 리스트
    private List<GameObject> playerCoins = new List<GameObject>();
    private List<GameObject> enemyCoins = new List<GameObject>();

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
        // 초기 UI 상태 설정
        if(skillPanel != null) skillPanel.SetActive(false);
        if(clashResultPanel != null) clashResultPanel.SetActive(false);
        if (battleResultPanel != null) battleResultPanel.SetActive(false);

        // 버튼 리스너 등록
        if (restartButton != null) restartButton.onClick.AddListener(OnRestartClicked);
        if (lobbyButton != null) lobbyButton.onClick.AddListener(OnLobbyclicked);

        if(optionPanel != null) optionPanel.SetActive(false); // 시작 시 숨김
    }
    private void Update()
    {
        if (isSelectingTarget)
        {
            HandleTargetInput();
        }
    }

    // === 턴 시작 시 호출 (CombatManager.SetState(StartTurn)에서 호출됨) ===
    public void ShowSkillSelection(CharacterStats actor)
    {
        currentActor = actor;
        clashResultPanel.SetActive(false); // 새로운 턴 시작 시 이전 합 결과 숨기기
        skillPanel.SetActive(true);

        // 필수 UI 필드 존재 여부 확인 (NRE 방지)
        if(skillPanel == null || skillButtonPrefab == null || skillButtonParent == null)
        {
            Debug.LogError("UIController의 필수 UI 필드 연결이 누락되었습니다. 상태 전환 실패");
            return;
        }
        // 이전 버튼 모두 파괴
        foreach(Transform child in skillButtonParent)
        {
            Destroy(child.gameObject);
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
    private void HandleTargetInput()
    {
        if(Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if(Physics.Raycast(ray, out RaycastHit hit, 100f, targetLayer))
            {
                CharacterView targetView = hit.collider.GetComponentInParent<CharacterView>();
                if(targetView != null && targetView.stats.CurrentHP > 0)
                {
                    isSelectingTarget = false;
                    CombatManager.Instance.OnSkillSelected(selectedSkill, targetView.stats);
                }
            }
        }
    }
    // CombatManager에서 호출될 메서드 (CS1061 오류 해결)
    public void ShowClashResult(string actorName, int actorPower, string targetName, int targetPower, int actorCoins, int targetCoins)
    {
        if(clashResultPanel == null || clashResultText == null) return;

        clashResultPanel.SetActive(true);

        // 코인 정보까지 포함하여 표시
        clashResultText.text = $"{actorName}({actorPower})\nVS\n{targetName}({targetPower})\n" +
                               $"\nCoins: {actorCoins} : {targetCoins}";
    }
    // 전투 종료 UI 메서드

    /// <summary>
    /// 전투가 끝났을 때 승리 또는 패배 창을 띄웁니다.
    /// </summary>
    public void ShowBattleResult(bool isVictory)
    {
        if(battleResultPanel == null) return;

        battleResultPanel.SetActive(true);

        if(resultTitleText != null)
        {
            // resultTitleText.text = isVictory ? "VICTORY" : "DEFEAT";
            // resultTitleText.color = isVictory ? Color.yellow : Color.red;

            battleResultPanel.transform.Find("VICTORY").gameObject.SetActive(isVictory);
            battleResultPanel.transform.Find("DEFEAT").gameObject.SetActive(!isVictory);
        }
    }
    public void UpdateClashCoins(int pCurrent, int pHeads, int eCurrent, int eHeads)
    {
        // 플레이어 코인 상태 업데이트
        for(int i = 0; i < playerCoins.Count; i++)
        {
            if(i >= pCurrent)
            {
                playerCoins[i].SetActive(false); // 파괴된 코인 숨기기
            }
            else
            {
                // i가 pHeads 보다 작으면 색상, 아니면 뒷면 색상
                Image img = playerCoins[i].GetComponent<Image>();
                img.color = (i < pHeads) ? Color.yellow : Color.gray;
            }
        }
        for(int i = 0; i < enemyCoins.Count; i++)
        {
            if (i >= eCurrent)
            {
                enemyCoins[i].SetActive(false); // 파괴된 코인 숨기기
            }
            else
            {
                // i가 eHeads 보다 작으면 색상, 아니면 뒷면 색상
                Image img = enemyCoins[i].GetComponent<Image>();
                img.color = (i < eHeads) ? Color.yellow : Color.gray;
            }
        }
    }
    private void OnRestartClicked()
    {
        // 현재 활성화된 씬을 다시 로드 (전투 초기화)
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);

        // 게임 재시작 시 시간 흐름을 1초로 초기화
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
    private void OnLobbyclicked()
    {
        // 메인 로비 씬을 이동 (씬 이름이 "Lobby" 라고 가정)
        // SceneManager.LoadScene("Lobby");
        Debug.Log("로비로 이동합니다.");
    }
    // 메뉴 버튼을 눌렀을 때 실행될 메서드
    public void ToggleOptionMenu()
    {
        if(optionPanel == null) return;

        // 현재 상태의 반대로 바꿈 (켜져 있으면 끄고 꺼져 있으면 켬)
        bool isAcitve = optionPanel.activeSelf;
        optionPanel.SetActive(!isAcitve);

        //메뉴가 열릴 때 게임을 일시정지
        Time.timeScale = isAcitve ? 1f : 0f;
    }
}