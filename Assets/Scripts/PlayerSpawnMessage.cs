using Mirror;

public struct PlayerSpawnMessage : NetworkMessage
{
    public int tireIndex;
    public string userId;
}