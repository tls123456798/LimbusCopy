// CharacterView.cs
using UnityEngine;

public class CharacterView : MonoBehaviour
{
    // 외부에서 설정되거나 CombatManager로부터 받아올 CharacterStats 데이터
    public CharacterStats stats;

    // 공격 애니메이션을 실행하는 메서드
    public void PlayAttackAnimation(string skillName)
    {
        // TODO: Animator 컴포넌트를 사용하여 공격 애니메이션 재생 로직 구현
        Debug.Log($"{stats.Id}가 {skillName}으로 공격 애니메이션 재생 시작");

        // 애니메이션이 끝나면 CombatManager에게 끝났다고 알려주는 콜백 필요 (다음 단계)
    }

    // 데미지 텍스트를 띄우고 HP 바를 업데이트하는 메서드
    public void UpdateHealthBar(int newHP)
    {
        // TODO: UI 컴포넌트(HP Bar)를 찾아서 체력 비율을 업데이트
        // TODO: 데미지 폰트 띄우기 (예: -15)
    }
}