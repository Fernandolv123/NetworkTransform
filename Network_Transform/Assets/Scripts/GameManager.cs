using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using Networking_Transformless;
public class GameManager : MonoBehaviour
{
    public static PLAYMODE PlayMode=PLAYMODE.ServerAutority;
    public static GameManager instance;
    void Awake(){
        instance = this;
    }
        void OnGUI()
        {
            GUILayout.BeginArea(new Rect(10, 10, 300, 300));
            if (!NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsServer)
            {
                StartButtons();
            }
            else
            {
                StatusLabels();
            }

            GUILayout.EndArea();
        }

        static void StartButtons()
        {
            if (GUILayout.Button("Host")) NetworkManager.Singleton.StartHost();
            if (GUILayout.Button("Client")) NetworkManager.Singleton.StartClient();
            if (GUILayout.Button("Server")) NetworkManager.Singleton.StartServer();
        }

        void StatusLabels()
        {
            var mode = NetworkManager.Singleton.IsHost ?
                "Host" : NetworkManager.Singleton.IsServer ? "Server" : "Client";

            GUILayout.Label("Transport: " +
                NetworkManager.Singleton.NetworkConfig.NetworkTransport.GetType().Name);
            if(mode != "Client")GUILayout.Label("Mode: " + (PlayMode==PLAYMODE.ServerAutority ? "ServerAuthority" : PlayMode==PLAYMODE.ClientAutority ? "ClientAuthority" : "ServerRewindAuthotity"));
            if (mode == "Host" || mode == "Server"){
                if (GUILayout.Button("ServerAuthority")) ChangePlayMode(PLAYMODE.ServerAutority);
                if (GUILayout.Button("ClientAuthority")) ChangePlayMode(PLAYMODE.ClientAutority);
                if (GUILayout.Button("ServerRewindAuthotity")) ChangePlayMode(PLAYMODE.ServerRewind);
            }
        }
        void ChangePlayMode(PLAYMODE mode){
            PlayMode = mode;
            foreach (ulong uid in NetworkManager.Singleton.ConnectedClientsIds)
                NetworkManager.Singleton.SpawnManager.GetPlayerNetworkObject(uid).GetComponent<Optimized_PlayerTransformless>().ChangeAutority(PlayMode);
        }
        public void GetPlayMode(NetworkVariable<PLAYMODE> network){
            network.Value = PlayMode;
        }
}
