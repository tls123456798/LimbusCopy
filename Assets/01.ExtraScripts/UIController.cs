using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class UIController : MonoBehaviour
{
    // === 싱글톤 인스턴스 ===
    public static UIController Instance { get; private set; }

    [Header("UI 요소")]
    public GameObject skillPanel;
    public Text turnInfoText;

    [Header("동적 스킬 UI 요소")]
    public GameObject skillButtonPrefab;
    public Transform skillButtonParent;

    [Header("타겟팅")]
    public LayerMask targetLayer;

    [Header("전투 결과 표시")]
    public GameObject clashResultPanel;
    public TextMeshProUGUI clashResultText;

    [Header("전투 종료 결과 UI")]
    public GameObject battleResultPanel;
    public TextMeshProUGUI resultTitleText;

    [Header("옵션 패널")]
    public GameObject optionPanel;
    public Button restartButton;
    public Button lobbyButton;

    [Header("코인 연출 요소")]
    public GameObject coinPrefab;
    public Transform playerCoinParent;
    public Transform enemyCoinParent;

    // 코인들을 관리할 리스트
    private List<GameObject> playerCoins = new List<GameObject>();
    private List<GameObject> enemyCoins = new List<GameObject>();

    private bool isSelectingTarget = false;
    private CharacterStats currentActor;
    private Skill selectedSkill;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        if (skillPanel != null) skillPanel.SetActive(false);
        if (clashResultPanel != null) clashResultPanel.SetActive(false);
        if (battleResultPanel != null) battleResultPanel.SetActive(false);
        if (optionPanel != null) optionPanel.SetActive(false);

        if (restartButton != null) restartButton.onClick.AddListener(OnRestartClicked);
        if (lobbyButton != null) lobbyButton.onClick.AddListener(OnLobbyclicked);
    }

    private void Update()
    {
        if (isSelectingTarget) HandleTargetInput();
    }

    /// <summary>
    /// [Limbus Style] 합 시작 시 코인들을 생성하고 '이미지는 숨긴 상태'로 대기합니다.
    /// </summary>
    public void InitializeClashUI(int playerMaxCoin, int enemyMaxCoin)
    {
        clashResultPanel.SetActive(true);
        ClearCoins();

        // 플레이어 코인 생성
        for (int i = 0; i < playerMaxCoin; i++)
        {
            if (coinPrefab == null) return;
            GameObject coin = Instantiate(coinPrefab, playerCoinParent);
            SetCoinVisibility(coin, false); // 처음엔 앞/뒷면 모두 숨김
            playerCoins.Add(coin);
        }

        // 적 코인 생성
        for (int i = 0; i < enemyMaxCoin; i++)
        {
            GameObject coin = Instantiate(coinPrefab, enemyCoinParent);
            SetCoinVisibility(coin, false); // 처음엔 앞/뒷면 모두 숨김
            enemyCoins.Add(coin);
        }
    }

    /// <summary>
    /// 코인 내부의 Front/Back 오브젝트를 모두 끄거나 켜는 헬퍼 함수
    /// </summary>
    private void SetCoinVisibility(GameObject coinObj, bool visible)
    {
        Transform f = coinObj.transform.Find("Front");
        Transform b = coinObj.transform.Find("Back");
        if (f != null) f.gameObject.SetActive(visible);
        if (b != null) b.gameObject.SetActive(visible);
    }

    private void ClearCoins()
    {
        foreach (var c in playerCoins) if (c != null) Destroy(c);
        foreach (var c in enemyCoins) if (c != null) Destroy(c);
        playerCoins.Clear();
        enemyCoins.Clear();
    }

    /// <summary>
    /// [Limbus Style] 전투(합)가 완전히 종료되었을 때 호출하여 코인 UI를 정리합니다.
    /// </summary>
    public void HideClashUI()
    {
        clashResultPanel.SetActive(false);
        ClearCoins();
    }

    // --- 기존 스킬 선택 및 타겟팅 로직 (유지) ---
    public void ShowSkillSelection(CharacterStats actor)
    {
        currentActor = actor;
        clashResultPanel.SetActive(false);
        skillPanel.SetActive(true);
        foreach (Transform child in skillButtonParent) Destroy(child.gameObject);
        if (turnInfoText != null) turnInfoText.text = $"{actor.Name}의 턴입니다.";
        foreach (var skill in actor.AvailableSkills)
        {
            GameObject buttonObj = Instantiate(skillButtonPrefab, skillButtonParent);
            buttonObj.GetComponentInChildren<TextMeshProUGUI>().text = skill.Name;
            buttonObj.GetComponent<Button>().onClick.AddListener(() => StartTargetSelection(skill));
        }
        CombatManager.Instance.SetState(CombatState.WaitingForInput);
    }

    private void StartTargetSelection(Skill skill)
    {
        skillPanel.SetActive(false);
        isSelectingTarget = true;
        selectedSkill = skill;
    }

    private void HandleTargetInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, 100f, targetLayer))
            {
                CharacterView targetView = hit.collider.GetComponentInParent<CharacterView>();
                if (targetView != null && targetView.stats.CurrentHP > 0)
                {
                    isSelectingTarget = false;
                    CombatManager.Instance.OnSkillSelected(selectedSkill, targetView.stats);
                }
            }
        }
    }

    public void ShowClashResult(string actorName, int actorPower, string targetName, int targetPower, int actorCoins, int targetCoins)
    {
        if (clashResultPanel == null || clashResultText == null) return;
        clashResultPanel.SetActive(true);
        clashResultText.text = $"{actorName}({actorPower})\nVS\n{targetName}({targetPower})\n" +
                               $"\nCoins: {actorCoins} : {targetCoins}";
    }

    public void ShowBattleResult(bool isVictory)
    {
        if (battleResultPanel == null) return;
        battleResultPanel.SetActive(true);
        Transform victoryObj = battleResultPanel.transform.Find("VICTORY");
        Transform defeatObj = battleResultPanel.transform.Find("DEFEAT");
        if (victoryObj != null) victoryObj.gameObject.SetActive(isVictory);
        if (defeatObj != null) defeatObj.gameObject.SetActive(!isVictory);
    }

    // --- 코인 업데이트 핵심 로직 ---

    public void UpdateClashCoins(int pCurrent, int pHeads, int eCurrent, int eHeads)
    {
        UpdateCoinObjects(playerCoins, pCurrent, pHeads);
        UpdateCoinObjects(enemyCoins, eCurrent, eHeads);
    }

    private void UpdateCoinObjects(List<GameObject> coinList, int currentCount, int headsCount)
    {
        for (int i = 0; i < coinList.Count; i++)
        {
            if (coinList[i] == null) continue;

            // 1. 합에서 패배하여 파괴된 코인은 아예 끔
            if (i >= currentCount)
            {
                coinList[i].SetActive(false);
                continue;
            }

            coinList[i].SetActive(true);
            Transform front = coinList[i].transform.Find("Front");
            Transform back = coinList[i].transform.Find("Back");

            if (front != null && back != null)
            {
                // [참고] headsCount는 현재까지 '앞면'이 나온 코인의 수입니다.
                // 보통 Limbus 방식에서는 i번째 코인이 던져졌을 때 결과를 보여줍니다.
                // 여기서는 i < headsCount 이면 앞면, 아니면 뒷면으로 표시합니다.
                bool isHead = (i < headsCount);
                front.gameObject.SetActive(isHead);
                back.gameObject.SetActive(!isHead);
            }
        }
    }

    private void OnRestartClicked()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void OnLobbyclicked()
    {
        Debug.Log("로비로 이동합니다.");
    }

    public void ToggleOptionMenu()
    {
        if (optionPanel == null) return;
        bool isAcitve = optionPanel.activeSelf;
        optionPanel.SetActive(!isAcitve);
        Time.timeScale = isAcitve ? 1f : 0f;
    }
}