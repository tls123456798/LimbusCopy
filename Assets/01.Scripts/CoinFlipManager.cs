using UnityEngine;

// 확률 계산만 담당하는 정적(Static) 클래스
public static class CoinFlipManager
{
    public static int GetSuccessCount(SkillData skill)
    {
        int successCount = 0;
        for (int i = 0; i< skill.coinflipCount; i++)
        {
            float randomValue = Random.value;
            if(randomValue < skill.successChance)
            {
                successCount++;
                Debug.Log($"코인 {i + 1}번째: 앞면(성공)!");
            }
            else
            {
                Debug.Log($"코인 {i + 1}번째: 뒷면(실패)!");
            }
        }
        return successCount;
    }
}
