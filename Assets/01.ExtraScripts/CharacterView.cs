using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class CharacterView : MonoBehaviour
{
    [Header("데이터 참조")]
    // 인스펙터에서 수동으로 CharacterStats 스크립터블 오브젝트나 인스턴스를 할당해야 함
    // 만약 CharacterStats가 Monobehaviour라면, 이 캐릭터 오브젝트의 자식에 붙어 있을수 있음
    public CharacterStats stats;

    [Header("시각적 요소")]
    public Animator animator;
    public Slider healthBarSlider; // HP를 표시할 UI Slider

    public void Start()
    {
        // 데이터 확인
        if (stats == null)
        {
            Debug.LogError($"CharacterView '{gameObject.name}'에 CharacterStats 데이터 가 할당되지 않았습니다!");
            return;
        }
        // CombatManager에 자신(View) 등록
        if (CombatManager.Instance != null)
        {
            CombatManager.Instance.RegisterView(this);
        }
        else
        {
            Debug.LogError("CombatManager 인스턴스를 찾을 수 없습니다. 씬에 CombatManager가 있는지 확인하세요.");
        }
        // 초기 HP 바 업데이트
        UpdateHealthBar();
    }
    /// <summary>
    /// HP 상태가 변경되었을 때 Health Bar UI를 업데이트합.
    /// </summary>
    public void UpdateHealthBar()
    {
        if(healthBarSlider != null)
        {
            // 최대 HP와 현재 HP를 이용하여 슬라이더 값을 설정
            healthBarSlider.maxValue = stats.MaxHP;
            healthBarSlider.value = stats.CurrentHP;
        }
        // 캐릭터가 사망했는지 체크
        if(stats.CurrentHP <= 0)
        {
            PlayDeathAnimation();
        }
    }
    /// <summary>
    /// 공격 애니메이션을 재생하고, 애니메이션이 끝난 후 CombatManager에게 알려줍니다.
    /// </summary>
    /// <param name="triggerName"> 재생할 애니메이션 트리거 이름</param>
    public void PlayAttackAnimation(string triggerName)
    {
        if(animator != null)
        {
            animator.SetTrigger(triggerName);
        }
        // 애니메이션 재생 시간만큼 지연 후 콜백을 호출해야 합니다.
        // 여기서는 임시로 1초 후 콜백을 호출하도록 코루틴을 사용
        StartCoroutine(WaitForAnimation(1.0f));
    }
    /// <summary>
    /// 사망 애니메이션을 재생하고 오브젝트를 비활성화합니다.
    /// </summary>
    public void PlayDeathAnimation()
    {
        if(animator != null)
        {
            animator.SetTrigger("Die"); // "Die" 트리거 가정
        }
        // TODO: 사망 애니메이션이 끝난 후 오브젝트 비활성화 로직 추가 (코루틴 사용 권장)
        this.gameObject.SetActive(false);
    }

    /// <summary>
    /// 애니메이션 재생을 기다렸다가 CombatManager에게 턴 종료를 알리느 코루틴
    /// </summary>
    private System.Collections.IEnumerator WaitForAnimation(float duration)
    {
        yield return new WaitForSeconds(duration);

        // 애니메이션이 끝나면 CombatManater에게 다음 턴으로 넘기라고 알립니다.
        if(CombatManager.Instance != null)
        {
            CombatManager.Instance.OnAnimationFinished();
        }
    }
}