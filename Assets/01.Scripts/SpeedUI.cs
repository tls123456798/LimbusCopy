using TMPro;
using UnityEngine;

public class SpeedUI : MonoBehaviour
{
    [SerializeField] private TMP_Text speed;
    private void Start()
    {
        int randomNumber = Random.Range(1, 6);
        if(speed.text != null)
        {
            speed.text = "" + randomNumber.ToString();
        }
        float randomFloat = Random.value;
    }
    public void UpdataSpeedText(int currentSpeed)
    {
        speed.text = currentSpeed.ToString();
    }
}
