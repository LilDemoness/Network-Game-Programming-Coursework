using Gameplay.GameplayObjects.Character.Customisation.Data;

[System.Serializable]
public class ClientData
{
    public ulong ClientID { get; }

    public BuildData BuildData { get; set; }


    private ClientData() { }
    public ClientData(ulong clientID) => ClientID = clientID;
}
