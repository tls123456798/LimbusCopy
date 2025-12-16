using System;
using System.Collections;
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
    public Animator animator; // 캐릭터 애니메이션 제어
    public Slider healthBarSlider; // HP를 표시할 UI Slider

    [Header("합/데미지 시각화")]
    public Text floatingDamageTextPrefab; // 프리팹으로 사용할 데미지 텍스트
    public Canvas canvasParent; // 텍스트를 띄울 캔버스 (씬에 있어야 함)

    public void Start()
    {
        if (CombatManager.Instance == null)
        {
            Debug.LogError("CombatManager 인스턴스를 찾을 수 없습니다. 씬에 CombatManager 가 있는지 확인하세요.");
        }
    }
    public void IntializeView()
    {
        // 데이터 확인
        if (stats == null)
        {
            Debug.LogError($"CharacterView '{gameObject.name}' 에 CharacterStats 데이터가 할당되지 않았습니다!");
            return;
        }
        if (healthBarSlider == null)
        {
            Debug.LogError($"'{gameObject.name}'의 CharacterView: HP Slider가 InsPector에 연결되지 않았습니다. HP 업데이트 불가.");
            return;
        }
        healthBarSlider.maxValue = stats.MaxHP;

        // 초기 HP바 업데이트
        UpdateHealthBar();
    }
    /// <summary>
    /// HP 상태가 변경되었을 때 Health Bar UI를 업데이트합.
    /// </summary>
    public void UpdateHealthBar()
    {
        if (healthBarSlider != null)
        {
            // 최대 HP와 현재 HP를 이용하여 슬라이더 값을 설정
            healthBarSlider.value = stats.CurrentHP;
        }
        // 캐릭터가 사망했는지 체크
        if (stats.CurrentHP <= 0)
        {
            PlayDeathAnimation();
        }
    }
    // 합 및 데미지 시각화 추가

    /// <summary>
    /// 데미지 수치를 캐릭터 위에 띄웁니다.
    /// </summary>
    public void ShowDamageText(int damage, Color color, float duration = 1f)
    {
        if(floatingDamageTextPrefab == null || canvasParent == null)
        {
            Debug.LogWarning($"[{gameObject.name}] 데미지 텍스트 프리팹 또는 캔버스 부모가 연결되지 않아 데미지 ({damage}) 시각화를 건너뜁니다.");
            return;
        }
        // 임시 텍스트 인스턴스화
        Text damageText = Instantiate(floatingDamageTextPrefab, canvasParent.transform);
        damageText.text = $"-{damage}";
        damageText.color = color;

        // 캐릭터의 위치 근처에 배치
        damageText.transform.position = transform.position + new Vector3(0, 2f, 0);

        // 일정 시간 후 제거하는 코루틴 시작
        StartCoroutine(FadeAndDestroyText(damageText.gameObject, duration));
    }

    /// <summary>
    /// 합 승리 시 일시적이 ㄴ시각 효과를 표시합니다.
    /// </summary>
    public void ShowClashVictoryEffect()
    {
        Debug.Log($"[{gameObject.name}] 합 승리 이펙트 재생 (임시: 색상 변경 등)");
        // 시각적 연출 구현: 예를 들어, 0.2초 동안 캐릭터 색상을 밝게 변경
        StartCoroutine(FlashColor(Color.yellow, 0.2f));
    }

    /// <summary>
    /// 합 패배시 일시적인 시각 효과를 표시합니다.
    /// </summary>
    public void ShowClashDefeatEffect()
    {
        Debug.Log($"[{gameObject.name}] 합 패배 이펙트 재생 (임시: 흔들림 등)");
        // 시각적 연출 구현: 예를 들어, 0.2초 동안 캐릭터 색상을 어둡게 변경
        StartCoroutine(FlashColor(Color.red, 0.2f));
    }

    /// <summary>
    /// 공격 애니메이션을 재생하고, 애니메이션이 끝난 후 CombatManager에게 알려줍니다.
    /// </summary>
    /// <param name="triggerName"> 재생할 애니메이션 트리거 이름</param>
    public void PlayAttackAnimation(string triggerName = "Attack")
    {
        float animationDuration = 0.5f; // 기본 애니메이션 재생 시간 (임시 값)

        if (animator != null)
        {
            animator.SetTrigger("Attack"); // "Attack" 트리거 가정
            // 실제 애니메이션 길이를 여기서 계산하거나 애니메이션 크립에서 가져와야 함
        }
        // 애니메이션 재생 후 CombataManager에게 완료 신호를 보냅니다.
        StartCoroutine(NotifyCombatManagerAfterDelay(animationDuration));
    }
    /// <summary>
    /// 사망 애니메이션을 재생하고 오브젝트를 비활성화합니다.
    /// </summary>
    public void PlayDeathAnimation()
    {
        if (animator != null)
        {
            animator.SetTrigger("Die"); // "Die" 트리거 가정
        }
        StartCoroutine(DisableAfterDelay(3.0f));
    }
    /// <summary>
    /// CombatManager 에게 애니메이션 완료를 통보하는 코루틴 입니다.
    /// </summary>
    private IEnumerator NotifyCombatManagerAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (CombatManager.Instance != null)
        {
            // 애니메이션이 끝나면 Combatmanager에게 다음 상태로 넘어가도록 알립니다.
            CombatManager.Instance.OnAnimationFinished();
        }
    }
    /// <summary>
    /// GameObject를 일정 시간 후 비활성화 합니다.
    /// </summary>
    private IEnumerator DisableAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        gameObject.SetActive(false);
    }
    /// <summary>
    /// UI 텍스트를 페이드 아웃 시킨 후 제거합니다.
    /// </summary>
    private IEnumerator FadeAndDestroyText(GameObject textObject, float duration)
    {
        // 간단화를 위해 바로 제거
        yield return new WaitForSeconds(duration);
        Destroy(textObject);
    }
    /// <summary>
    /// 임시 시각 효과를 위해 캐릭터 색상을 잠시 변경합니다.
    /// </summary>
    private IEnumerator FlashColor(Color color, float duration)
    {
        // 실제 캐릭터 렌더러 컴포넌트 (MeshRender/SkinnedMeshRenderer)를 찾아서
        // Material.color를 변경하는 로직이 필요합니다.

        Renderer renderer = GetComponentInChildren<Renderer>();
        Color originalColor = renderer != null ? renderer.material.color : Color.white;

        if(renderer != null)
        {
            renderer.material.color = color;
            yield return new WaitForSeconds(duration);
            renderer.material.color = originalColor; // 원래 색상으로 복원
        }
        else
        {
            yield return null;
        }
    }
}
