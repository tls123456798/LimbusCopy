// Combatmanager.cs 파일 안에 정의

public enum CombatState
{
    // 초기화 단계
    Setup,
    // 1. 턴 순서 결정 및 UI 설정
    StartTurn,
    // 2. 아군 입력 대기 (스킬/대상 선택)
    WaitingForInput,
    // 3. 턴 시작 시 합이 발생하는지 확인하고 준비 하는 단계
    ClashSetup,
    // 4. 실제로 합 위력을 굴리고 승패를 판정하는 단계
    ClashCalculation,
    // 5. 전투 계산 (합 판정, 데미지 계산)
    CalculatingCombat,
    // 6. 애니메이션 재생 및 결과 표시
    PlayingAnimation,
    // 7. 전투 종료 (승리/패배 체크)
    CombatEnd
}
