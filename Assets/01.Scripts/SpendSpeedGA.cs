using UnityEngine;

public class SpendSpeedGA : GameAction
{
    public int Amount {  get; private set; }
    public SpendSpeedGA(int amount)
    {
        Amount = amount;
    }
}
