using UnityEngine;
using System.Collections.Generic;

[System.Serializable] // 유니티 인스펙터에 표시되도록 설정

public class CharacterStats
{
    public List<Skill> AvailableSkills = new List<Skill>(); // 사용할 수 있는 스킬 목록
    public List<StatusEffect> ActiveEffects = new List<StatusEffect>(); // 현재 적용된 상태 이상
   
    // 캐릭터의 고유 ID (어떤 캐릭터인지 식별)
    public string Id;

    // 기본 능력치
    public int MaxHP;
    public int CurrentHP;
    public int Attack;
    public int Speed; // 턴 순서 결정에 사용
    public int Defender;

    // 스킬 정보 (코인, 위력 등을 담을 구조체)
    public List<string> Skills;

    // 생성자 (Constructor)
    public CharacterStats(string id, int hp, int atk, int spd, int def)
    {
        this.Id = id;
        this.MaxHP = hp;
        this.CurrentHP = hp;
        this.Attack = atk;
        this.Speed = spd;
        this.Defender = def;
    }

    public object Resistances { get; internal set; }
    public object SkillCooldowns { get; internal set; }

    // 메서드 예시: 데미지 받기
    public void TakeDamage(int damage)
    {
        CurrentHP -= damage;
        if (CurrentHP < 0) CurrentHP = 0;

        UnityEngine.Debug.Log($"{damage} 데미지를 받았습니다. 남은 HP: {CurrentHP}");
    }
    public enum SinAttribute {red,orange,yellow,green,blue,deepblue,puple } // 속성 예시(추후 교체)

    // 상태 이상 정보를 담아 두는 구조체/클래스 정의
    public class StatusEffect
    {
        public string Name; // 상태이상 이름
        public int Stacks; // 중첩 횟수
        public int Duration; // 남은 지속 시간 (턴 수)
    }
    public class CharacterSTats
    {
        // 새로운 핵심 전투 필드
        public SinAttribute AttackSinType; // 현재 스킬의 공격 속성
        public Dictionary<SinAttribute, float> Resistances; // 속성별 내성 계수

        public Dictionary<string, int> SkillCooldowns = new Dictionary<string, int>(); // 스킬 이름별 쿨타임
        public Dictionary<SinAttribute, int> EgoResources = new Dictionary<SinAttribute, int>(); // 죄악 자원 보유량

        public List<StatusEffect> ActiveEffects = new List<StatusEffect>(); // 현재 걸린 상태 이상 목록
    }
}
