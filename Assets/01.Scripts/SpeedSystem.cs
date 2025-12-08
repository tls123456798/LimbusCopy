using System.Collections;
using System.Runtime.CompilerServices;
using UnityEngine;

public class SpeedSystem : Singleton<SpeedSystem>
{
    [SerializeField] private SpeedUI speedUI;
    private const int MAX_SPEED = 99;
    private int currentSpeed = MAX_SPEED;
    private IEnumerator SpendSpeedPerformer(SpendSpeedGA spendSpeedGA)
    {
        currentSpeed = spendSpeedGA.Amount;
        speedUI.UpdataSpeedText(currentSpeed);
        yield return null;
    }
}

