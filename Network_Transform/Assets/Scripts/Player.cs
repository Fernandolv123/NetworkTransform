using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]
public class Player : NetworkBehaviour
{   
    public static NetworkVariable<int> PlayMode2 = new NetworkVariable<int>();
    public int PlayMode;
    protected int zVelocity;
    protected int xVelocity;
    private Rigidbody rb;
    public float speed = 4;
    public float jumpForce;

    void Awake() {
        rb = GetComponent<Rigidbody>();
    }

    public override void OnNetworkSpawn()
    {
        Debug.Log("{CLIENT} Spawned");
        if (IsOwner)
        {
            //Cambiamos su posicion inicial
            SubmitInitialPositionRPC();
        }
    }
    public void ChangeAutority(int mode){
        SubmitNewAutorityRPC(mode);
    }
    [Rpc(SendTo.Server)]
    void SubmitNewAutorityRPC(int mode){
        PlayMode2.Value = mode;
    }

    //Metodo de movimiento para server autority
    private void MovementServerAutority()
    {
        //Calculamos la dirección de movimiento del player
        zVelocity = Input.GetKey(KeyCode.W) ? 1 : Input.GetKey(KeyCode.S) ? -1 : 0;
        xVelocity = Input.GetKey(KeyCode.D) ? 1 : Input.GetKey(KeyCode.A) ? -1 : 0;

        SubmitPositionRequestServerRpc(zVelocity,xVelocity);
    
    }
    //Metodo de movimiento para client Autority
    private void MovementClientAutority()
    {
        //Calculamos la dirección de movimiento del player
        zVelocity = Input.GetKey(KeyCode.W) ? 1 : Input.GetKey(KeyCode.S) ? -1 : 0;
        xVelocity = Input.GetKey(KeyCode.D) ? 1 : Input.GetKey(KeyCode.A) ? -1 : 0;

        Vector3 newPosition = transform.position;
        newPosition.x = Mathf.Clamp(transform.position.x + xVelocity*Time.fixedDeltaTime* speed,-5,5);
        newPosition.z = Mathf.Clamp(transform.position.z + zVelocity*Time.fixedDeltaTime*speed,-5,5);
        transform.position = newPosition;
        //SubmitNewPositionServerRpc(newPosition);
    
    }
    private void MovementServerRewind()
    {
        //Calculamos la dirección de movimiento del player
        zVelocity = Input.GetKey(KeyCode.W) ? 1 : Input.GetKey(KeyCode.S) ? -1 : 0;
        xVelocity = Input.GetKey(KeyCode.D) ? 1 : Input.GetKey(KeyCode.A) ? -1 : 0;

        Vector3 newPosition = transform.position;
        newPosition.x = Mathf.Clamp(transform.position.x + xVelocity*Time.fixedDeltaTime* speed,-5,5);
        newPosition.z = Mathf.Clamp(transform.position.z + zVelocity*Time.fixedDeltaTime*speed,-5,5);
        transform.position = newPosition;
        SubmitNewPositionServerRpc(newPosition);
    
    }
    public void Jump() {
        Debug.Log("Salta");
        SubmitJumpRPC();
    }

    [Rpc(SendTo.Server)]
    void SubmitPositionRequestServerRpc(float moveZ,float moveX)
    {
        //Desgraciadamente, no podemos hacer un clamp de transform.Translate por como funciona, lo que significa que debemos cambiar la logica para utilizar newPosition
        Vector3 newPosition = transform.position;
        newPosition.x = Mathf.Clamp(transform.position.x + moveX*Time.fixedDeltaTime* speed,-5,5);
        newPosition.z = Mathf.Clamp(transform.position.z + moveZ*Time.fixedDeltaTime*speed,-5,5);
        transform.position = newPosition;
    }
    [Rpc(SendTo.Server)]
    void SubmitNewPositionServerRpc(Vector3 newPosition)
    {
        //Desgraciadamente, no podemos hacer un clamp de transform.Translate por como funciona, lo que significa que debemos cambiar la logica para utilizar newPosition
        //transform.position = newPosition;
    }

    //Creamos un método para cambiar la posición inicial y ponerlo al nivel del plano
    [Rpc(SendTo.Server)]
    void SubmitInitialPositionRPC(){
        transform.position = transform.position + Vector3.up;
    }

    //Creamos un método para mandar al servidor la orden de saltar
    [Rpc(SendTo.Server)]
    void SubmitJumpRPC(){
        rb.AddForce(Vector3.up*jumpForce,ForceMode.Impulse);
    }

    //El FixedUpdate no es llamado cada frame
    void FixedUpdate()
    {
        if (IsOwner){
            Debug.Log(PlayMode);
            switch (PlayMode){
                case 1:
                //ServerAutority
                MovementServerAutority();
                break;
                case 2:
                //ClientAutority
                MovementClientAutority();
                break;
                case 3:
                //ServerAutorityRewind
                MovementServerRewind();
                break;
            }
        }
    }
    void Update(){
        if (IsOwner){
            if (Input.GetKeyDown(KeyCode.Space)){
                Jump();
            }
        }
    }
}
