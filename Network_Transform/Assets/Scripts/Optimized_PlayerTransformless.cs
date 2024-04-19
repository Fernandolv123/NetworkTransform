using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class Optimized_PlayerTransformless : NetworkBehaviour
{
    //Creamos una variable de autoridad en cliente
    private NetworkVariable<Vector3> PositionControlledByClient = new NetworkVariable<Vector3>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    //Creamos una variable gobernada por el servidor
    private NetworkVariable<Vector3> PositionControlledByServer = new NetworkVariable<Vector3>();
    
    void Awake(){
        //subscribimos las NetworksVariables a los delegates OnValueChange, que son llamados al detectar un cambio de valor
        PositionControlledByClient.OnValueChanged += OnValueChangeClientVariable;
        PositionControlledByClient.OnValueChanged += OnValueChangeServerVariable;
    }
    //OnNetworkSpawn es llamada cada vez que se Spawnea Un Optimized_PlayerTransformless
    public override void OnNetworkSpawn()
    {
        if(IsOwner){
            
            //GetPlayModeRPC();
            //Cambiamos la posición inicial
            SubmitInitialPositionRPC();
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    [Rpc(SendTo.Server)]
    void SubmitInitialPositionRPC(){
        transform.position = transform.position + Vector3.up;
    }

    public void OnValueChangeClientVariable(Vector3 previousValue, Vector3 newValue){

    }
    public void OnValueChangeServerVariable(Vector3 previousValue, Vector3 newValue){

    }
}
public enum PlayMode{
    ServerAutority,
    ClientAutority,
    ServerRewind
}
