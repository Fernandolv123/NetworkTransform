using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]
public class Player : NetworkBehaviour
{   
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
        if (IsOwner)
        {
            SubmitInitialPositionRPC();
        }
    }

    private void Movement()
    {
        zVelocity = Input.GetKey(KeyCode.W) ? 1 : Input.GetKey(KeyCode.S) ? -1 : 0;
        xVelocity = Input.GetKey(KeyCode.D) ? 1 : Input.GetKey(KeyCode.A) ? -1 : 0;

        //float moveX = Input.GetAxis("Horizontal");
        //float moveZ = Input.GetAxis("Vertical");

        //Vector3 newPosition = new Vector3(moveX,0,moveZ);
        //transform.position += newPosition * speed * Time.deltaTime;
        SubmitPositionRequestServerRpc(zVelocity,xVelocity);
    
    }

    public void Jump() {
        Debug.Log("Salta");
        SubmitJumpRPC();
    }

    [Rpc(SendTo.Server)]
    void SubmitPositionRequestServerRpc(float moveZ,float moveX)
    {
        //movemos el player en servidor
        transform.Translate(Vector3.forward * moveZ*Time.fixedDeltaTime * speed + Vector3.right * moveX*Time.fixedDeltaTime* speed,Space.Self);
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
}
