using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]
public class Player : NetworkBehaviour
{   
    public static NetworkVariable<int> PlayMode = new NetworkVariable<int>();
    //public int PlayMode;
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
            GetPlayModeRPC();
        }
    }
    [Rpc(SendTo.Server)]
    void GetPlayModeRPC(){
        GameManager.instance.GetPlayMode(PlayMode);
    }
    public void ChangeAutority(int mode){
        SubmitNewAutorityRPC(mode);
    }
    [Rpc(SendTo.Server)]
    void SubmitNewAutorityRPC(int mode){
        PlayMode.Value = mode;
    }

    private void Movement(){
        //Calculamos la dirección de movimiento del player
        zVelocity = Input.GetKey(KeyCode.W) ? 1 : Input.GetKey(KeyCode.S) ? -1 : 0;
        xVelocity = Input.GetKey(KeyCode.D) ? 1 : Input.GetKey(KeyCode.A) ? -1 : 0;
        if(PlayMode.Value == 1){
            //ServerAutority
            SubmitPositionRequestServerRpc(zVelocity,xVelocity);
        } else if (PlayMode.Value == 3){
            //ServerRewind
            Vector3 newPosition = transform.position;
            newPosition.x = transform.position.x + xVelocity*Time.fixedDeltaTime* speed;
            newPosition.z = transform.position.z + zVelocity*Time.fixedDeltaTime*speed;
            transform.position = newPosition;

            SubmitNewPositionRpc(newPosition);
        } else if (PlayMode.Value == 2){
            Vector3 newPosition = transform.position;
            newPosition.x = transform.position.x + xVelocity*Time.fixedDeltaTime* speed;
            newPosition.z = transform.position.z + zVelocity*Time.fixedDeltaTime*speed;
            transform.position = newPosition;
            //Debug.Log("{Movement} Nueva Posicion: "+newPosition);

            //transform.Translate(Vector3.forward * zVelocity*Time.fixedDeltaTime * speed + Vector3.right * xVelocity*Time.fixedDeltaTime* speed,Space.Self);
        }
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
    [Rpc(SendTo.ClientsAndHost)]
    void SubmitNewPositionRpc(Vector3 newPosition)
    {
        transform.position = newPosition;
        if (newPosition.x >= 5){
            //transform.position = Vector3.up;
            transform.position = new Vector3(transform.position.x-0.1f,transform.position.y,transform.position.z);
        } else if (newPosition.z >= 5){
            transform.position = new Vector3(transform.position.x,transform.position.y,transform.position.z-0.1f);
        } else if (newPosition.x <= -5){
            transform.position = new Vector3(transform.position.x+0.1f,transform.position.y,transform.position.z);
        } else if (newPosition.z <= -5){
            transform.position = new Vector3(transform.position.x,transform.position.y,transform.position.z+0.1f);
        }
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
            Debug.Log(PlayMode.Value);
            /*switch (PlayMode.Value){
                case 1:
                //ServerAutority
                //hacer transicion entre escenas
                //networkTransform.IsServerAuthoritative(true);
                ChangeAutority(true);
                //networkTransform.ChangeAutorityRPC(true);
                break;
                case 2:
                //ClientAutority
                //networkTransform.IsServerAuthoritative(false);
                ChangeAutority(false);
                //networkTransform.ChangeAutorityRPC(false);
                break;
                case 3:
                //ServerAutorityRewind
                //networkTransform.IsServerAuthoritative(false);
                ChangeAutority(false);
                //networkTransform.ChangeAutorityRPC(false);
                break;
            }*/
            Movement();
        }
    }
    void Update(){
        if (IsOwner){
            if (Input.GetKeyDown(KeyCode.Space)){
                Jump();
            }
        }
    }
    public void ChangeAutority(bool state){
        Debug.Log("Entra");
        if(state){
            Destroy(gameObject.GetComponent<CustomNetworkTransform>());
            gameObject.AddComponent<NetworkTransform?>();
        } else {
            Destroy(gameObject.GetComponent<NetworkTransform>());
            gameObject.AddComponent<CustomNetworkTransform?>();
        }
    }
}
