using System.Collections.Generic;
using Mirror;

public static class PlayerSessionManager
{
    private static Dictionary<NetworkConnection, string> sessionMap = new();

    public static void Register(NetworkConnection conn, string userId)
    {
        if (!sessionMap.ContainsKey(conn))
            sessionMap[conn] = userId;
    }

    public static string GetUserId(NetworkConnection conn)
    {
        return sessionMap.TryGetValue(conn, out var id) ? id : null;
    }

    public static string GetOpponentId(string excludeId)
    {
        foreach (var pair in sessionMap)
        {
            if (pair.Value != excludeId)
                return pair.Value;
        }
        return null;
    }

    public static void Clear()
    {
        sessionMap.Clear();

    }
}