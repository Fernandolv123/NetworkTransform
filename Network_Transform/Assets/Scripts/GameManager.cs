using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static int PlayMode=1;
    public static GameManager instance;
    void Awake(){
        instance = this;
    }
    void Update(){
        //PlayMode2.Value = PlayMode;
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

                SubmitNewPosition();
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
            GUILayout.Label("Mode: " + (PlayMode==1 ? "ServerAuthority" : PlayMode==2 ? "ClientAuthority" : "ServerRewindAuthotity"));
            if (mode == "Host" || mode == "Server"){
                if (GUILayout.Button("ServerAuthority")) ChangePlayMode(1);
                if (GUILayout.Button("ClientAuthority")) ChangePlayMode(2);
                if (GUILayout.Button("ServerRewindAuthotity")) ChangePlayMode(3);
            }
        }

        static void SubmitNewPosition()
        {
            if (GUILayout.Button(NetworkManager.Singleton.IsHost ? "Jump": NetworkManager.Singleton.IsServer ? "Jump But Better Cause you are server" : "Jump"))
            {
                if (NetworkManager.Singleton.IsServer && !NetworkManager.Singleton.IsClient )
                {
                    foreach (ulong uid in NetworkManager.Singleton.ConnectedClientsIds)
                        NetworkManager.Singleton.SpawnManager.GetPlayerNetworkObject(uid).GetComponent<Player>().Jump();
                }
                else
                {
                    var playerObject = NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject();
                    var player = playerObject.GetComponent<Player>();
                    player.Jump();
                }
            }
        }
        void ChangePlayMode(int mode){
            PlayMode = mode;
            foreach (ulong uid in NetworkManager.Singleton.ConnectedClientsIds)
                NetworkManager.Singleton.SpawnManager.GetPlayerNetworkObject(uid).GetComponent<Player_Transformless>().ChangeAutority(PlayMode);
        }
        public void GetPlayMode(NetworkVariable<int> network){
            network.Value = PlayMode;
        }
}
