using System.Collections;
using System.Collections.Generic;
using Unity.Netcode.Components;
using UnityEngine;

public class CustomNetworkTransform : NetworkTransform
{
    private static bool ServerAut = true;
    // Start is called before the first frame update
    public bool IsServerAuthoritative()
    {
        return ServerAut;
    }
    public void IsServerAuthoritative(bool state){
        ServerAut = state;
    }
}
