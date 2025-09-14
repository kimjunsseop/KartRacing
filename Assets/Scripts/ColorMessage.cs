using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public struct ColorMessage : NetworkMessage
{
    public Color color;
    public string userId;
}