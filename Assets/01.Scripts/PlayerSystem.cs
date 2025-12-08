using UnityEngine;

public class PlayerSystem : Singleton<PlayerSystem>
{
    [field: SerializeField] public PlayerView PlayerView {  get; private set; }
    public void Setup(PlayerData playerData)
    {
        PlayerView.Setup(playerData);
    }
}
