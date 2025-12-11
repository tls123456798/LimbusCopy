using UnityEngine;

public enum EffectType { Buff, Debuff}
public enum StatType { Attack, Defense, Speed, DamageOverTime}

[System.Serializable] // 유니티 인스펙터에 표시되도록 설정
public class StatusEffect
{
    public string Name; // 효과이름
    public EffectType Type; // 버프인지 디버프인지
    public StatType TargetStat; // 영향을 줄 스탯
    public int Duration; // 남은 턴 수
    public float Value; // 스태셍 적용할 값 또는 비율

    // 이펙트 생성자
    public StatusEffect(string name, EffectType type, StatType targetStat, int duration, float value)
    {
        Name = name;
        Type = type;
        TargetStat = targetStat;
        Duration = duration;
        Value = value;
    }
}
