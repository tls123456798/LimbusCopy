using UnityEngine;

public class MatchSetupSystem : MonoBehaviour
{
    [SerializeField] private PlayerData playerData;
    private void Start()
    {
        PlayerSystem.Instance.Setup(playerData);
    }
}
