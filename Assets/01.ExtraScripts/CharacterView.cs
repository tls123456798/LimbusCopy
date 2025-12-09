// CharacterView.cs
using UnityEngine;
using UnityEngine.InputSystem.XR.Haptics;
using UnityEngine.UI;

public class CharacterView : MonoBehaviour
{
    // 외부에서 설정되거나 CombatManager로부터 받아올 CharacterStats 데이터
    public CharacterStats stats;

    // 애니메이터 컴포넌트
    [SerializeField] private Animator animator;

    // CombatManager 인스턴스
    private CombatManager combatManager;

    private void Start()
    {
        // CombatManager.Instance 가 이미 싱글토능로 설정되어 있다고 가정
        combatManager = CombatManager.Instance;
    }

    // 공격 애니메이션을 실행하는 메서드
    public void PlayAttackAnimation(string skillName)
    {
        // TODO: Animator 컴포넌트를 사용하여 공격 애니메이션 재생 로직 구현
        Debug.Log($"{stats.Id}가 {skillName}으로 공격 애니메이션 재생 시작");

        // 애니메이션이 끝나면 CombatManager에게 끝났다고 알려주는 콜백 필요 (다음 단계)
        animator.SetTrigger(skillName);

        // 테스트용
        Invoke(nameof(OnAnimationCompleted), 2f);
    }
    public void OnAnimationCompleted()
    {
        // CombatManager의 콜백 메서드를 호출하여 상태 전환을 유도합니다.
        combatManager.OnAnimationFinished();
    }
    // 데미지 텍스트를 띄우고 HP 바를 업데이트하는 메서드
    public void UpdateHealthBar(int newHP)
    {
        // TODO: UI 컴포넌트(HP Bar)를 찾아서 체력 비율을 업데이트
        // TODO: 데미지 폰트 띄우기 (예: -15)
    }
    public Slider healthSlider; // 유니티 인스펙터에서 HP 바 UI 조정
    public void UpdateHealthBar()
    {
        // MaxHP 와 CurrentHP 비율을 슬라이더의 Value에 반영
        float ratio = (float)stats.CurrentHP / stats.MaxHP;
        healthSlider.value = ratio;

        // TODO: 데미지 텍스트 팝업 연출 추가
    }
}