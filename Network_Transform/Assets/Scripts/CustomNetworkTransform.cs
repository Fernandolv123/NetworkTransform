using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

public class CustomNetworkTransform : NetworkTransform
{
    // Start is called before the first frame update
    protected override bool OnIsServerAuthoritative()
    {
        return false;
    }
}
