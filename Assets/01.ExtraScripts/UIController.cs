using System.Collections.Generic;
using UnityEngine;

public class UIController : MonoBehaviour
{
    // 싱글턴 패턴
    public static UIController instance;

    public GameObject skillPanel; // 스킬 버튼을 담는 UI 패널
    //... HP Bar, Turn Indicator 등 필요한 UI 컴포넌트 변수...

    // CombatManager가 "입력 대기 상태" 일 때 호출하는 메서드
    public void ShowSkillSelection(List<string> skills)
    {
        skillPanel.SetActive(true);
        // TODO: skills 목록을 바탕으로 버튼들을 생성/갱신하는 로직
    }
    // 스킬 버튼에 연결될 메서드
    public void OnSkillButtonClicked(string skillName)
    {
        // 유저 입력을 CombatManager 의 메서드로 전달
        // Target 선택 로직이 필요하지만, 일단 간단하게 첫 번째 적으 타겟으로 가정

        skillPanel.SetActive(false); // 입력이 끝났으니 UI 숨기기
    }
    // 전투 결과, 데미지 표시 등의 메서드 추가
}
