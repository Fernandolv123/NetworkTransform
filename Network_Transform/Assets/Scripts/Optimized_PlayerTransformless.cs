using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace Networking_Transformless
{
    public class Optimized_PlayerTransformless : NetworkBehaviour
    {
        [Header("Player Movement")]
        public float speed;
        [Header("Synchronization")]
        //Create a client-controlled variable.
        private NetworkVariable<Vector3> PositionControlledByClient = new NetworkVariable<Vector3>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        //Create a server-controlled variable.
        private NetworkVariable<Vector3> PositionControlledByServer = new NetworkVariable<Vector3>();
        //Create a variable to detect when a position correction is needed for ServerRewind.
        public NetworkVariable<bool> serverGetsAutority = new NetworkVariable<bool>();
        [Header("Regarding movement")]
        private int zVelocity;
        private int xVelocity;
        public NetworkVariable<PLAYMODE> playmode = new NetworkVariable<PLAYMODE>();
        void Awake()
        {
            //OnValueChange delegates are called upon detecting a value change
            PositionControlledByClient.OnValueChanged += OnValueChangeClientVariable;
            PositionControlledByClient.OnValueChanged += OnValueChangeServerVariable;
        }
        //OnNetworkSpawn is called whenever a Optimized_PlayerTransformless is Spawned 
        public override void OnNetworkSpawn()
        {
            if (IsOwner)
            {
                //Check current PLAYMODE
                GetPlayModeRPC();
                //Set initial position
                Invoke("SubmitInitialPositionRPC", 0.3f); //Sometimes Client doesn't change its initial position, invoke makes it more consistent
            }
        }
        //Update PLAYMODE for the first time
        [Rpc(SendTo.Server)]
        void GetPlayModeRPC()
        {
            GameManager.instance.GetPlayMode(playmode);
        }
        //Server tells clients to manually change the game mode for everyone.
        public void ChangeAutority(PLAYMODE mode)
        {
            SubmitNewAutorityRPC(mode);
        }
        //Update PLAYMODE
        [Rpc(SendTo.Server)]
        void SubmitNewAutorityRPC(PLAYMODE mode)
        {
            playmode.Value = mode;
        }

        void Update()
        {
            //Owner Movement
            if (IsOwner)
            {
                Movement();
            }
            Debug.Log(playmode.Value);
            switch (playmode.Value)
            {
                case PLAYMODE.ServerAutority:
                    //Set the position to the server's position
                    transform.position = PositionControlledByServer.Value;

                    //Change client-controlled position to prevent teleportation upon changing modes
                    if (IsOwner) PositionControlledByClient.Value = transform.position;
                    break;
                case PLAYMODE.ClientAutority:
                    //Position is setted in OnValueChangeClientVariable method
                    //Sync variables on server
                    if (IsOwner) SyncPositionRpc();
                    break;
                case PLAYMODE.ServerRewind:
                    //The logic is as follows: we need to update our position to the server-controlled position, but we can't simply assign it,
                    //as that would mean we'd be working with serverAuthority but less efficiently.

                    //if server doesn't request any change, we have nothing to do
                    if (!serverGetsAutority.Value)
                    {
                        break;
                    }
                    //Update serverPosition
                    transform.position = PositionControlledByServer.Value;
                    break;
            }
        }
        private void Movement()
        {
            zVelocity = Input.GetKey(KeyCode.W) ? 1 : Input.GetKey(KeyCode.S) ? -1 : 0;
            xVelocity = Input.GetKey(KeyCode.D) ? 1 : Input.GetKey(KeyCode.A) ? -1 : 0;
            switch (playmode.Value)
            {
                case PLAYMODE.ServerAutority:
                    //In this case, we only need to change the server-controlled variable,
                    //so we need to create an RPC method to execute on the server
                    //(as the client doesn't have write permissions on this variable).
                    
                    SubmitPositionRequestServerRpc(zVelocity, xVelocity);
                    break;
                case PLAYMODE.ClientAutority:
                    // In this case, we only need to change the client-controlled variable,
                    // so it's not necessary to use any RPC method.
                    // In fact, this variable cannot be changed on the server as it doesn't have write permissions.
                    
                    PositionControlledByClient.Value = MoveNewPosition();
                    transform.position = PositionControlledByClient.Value;
                    break;
                case PLAYMODE.ServerRewind:
                    // In this case, where we have to change both variables,
                    // the logic is simple. First, we change the client-controlled variable.
                    // Then, if the new position interferes with the boundaries set on the server,
                    // we will change the position to an absolute position controlled by the server variable.

                    //Move locally
                    PositionControlledByClient.Value = MoveNewPosition();
                    transform.position = PositionControlledByClient.Value;

                    //Check position on server
                    SubmitPermitedPositionRpc(zVelocity, xVelocity);
                    break;
            }

        }
        private Vector3 MoveNewPosition()
        {
            Vector3 newPosition = transform.position;
            newPosition.x = transform.position.x + xVelocity * Time.deltaTime * speed;
            newPosition.z = transform.position.z + zVelocity * Time.deltaTime * speed;
            return newPosition;
        }
        [Rpc(SendTo.Server)]
        private void SubmitPositionRequestServerRpc(float moveZ, float moveX)
        {
            Vector3 newPosition = transform.position;
            newPosition.x = Mathf.Clamp(transform.position.x + moveX * Time.deltaTime * speed, -5, 5);
            newPosition.z = Mathf.Clamp(transform.position.z + moveZ * Time.deltaTime * speed, -5, 5);
            PositionControlledByServer.Value = newPosition;
        }
        [Rpc(SendTo.Server)]
        private void SubmitPermitedPositionRpc(float moveZ, float moveX)
        {
            //Server does not get autority at first
            serverGetsAutority.Value = false;
            Vector3 newPosition = transform.position;
            newPosition.x = Mathf.Clamp(transform.position.x + moveX * Time.deltaTime * speed, -5, 5);
            newPosition.z = Mathf.Clamp(transform.position.z + moveZ * Time.deltaTime * speed, -5, 5);
            PositionControlledByServer.Value = newPosition;
            if (OutOfBoundaries(newPosition))
            {
                //if player position is out of boundaries, server gets autority to change its posiition
                serverGetsAutority.Value = true;
                return;
            }
        }
        public bool OutOfBoundaries(Vector3 newPosition)
        {
            if (newPosition.x >= 5 || newPosition.z >= 5 || newPosition.x <= -5 ||newPosition.z <= -5)
            {
                return true;
            }
            return false;
        }

        [Rpc(SendTo.Server)]
        private void SubmitInitialPositionRPC()
        {
            if (playmode.Value == PLAYMODE.ServerAutority)
            {
                PositionControlledByServer.Value = Vector3.up;
            }
            else
            {
                transform.position = Vector3.up;
            }
        }
        [Rpc(SendTo.Server)]
        private void SyncPositionRpc()
        {
            PositionControlledByServer.Value = transform.position;
        }

        public void OnValueChangeClientVariable(Vector3 previousValue, Vector3 newValue)
        {
            if (playmode.Value == PLAYMODE.ServerAutority) return; //We don't want any change in serverAutority mode

            //If client has left boundaries, we set clients variable to that of the server
            if (serverGetsAutority.Value)
            {
                if (IsOwner) PositionControlledByClient.Value = PositionControlledByServer.Value;
                return;
            }
            //Set position for Synchronization between players
            transform.position = PositionControlledByClient.Value;
        }
        public void OnValueChangeServerVariable(Vector3 previousValue, Vector3 newValue)
        {
            //If Client is inside boundaries, skip
            if (!serverGetsAutority.Value)
            {
                return;
            }
            transform.position = PositionControlledByServer.Value;
        }
    }
    public enum PLAYMODE
    {
        ServerAutority,
        ClientAutority,
        ServerRewind
    }
}
