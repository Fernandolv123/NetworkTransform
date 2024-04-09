using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class Player : NetworkBehaviour
{
    protected int upwardsVelocity;
    protected int horizontalVelocity;
    public float speed = 4;
    //cambiado en el clon

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            //Move();
        }
    }

    public void Move()
    {
    if(Input.GetKey(KeyCode.W)){
        upwardsVelocity = 1;
    } else if(Input.GetKey(KeyCode.S)){
        upwardsVelocity = -1;
    } else {
        upwardsVelocity = 0;
    }

    if(Input.GetKey(KeyCode.A)){
        horizontalVelocity = 1;
    } else if(Input.GetKey(KeyCode.D)){
        horizontalVelocity = -1;
    } else {
        horizontalVelocity = 0;
    }
        //transform.Translate(Vector3.forward * upwardsVelocity*Time.fixedDeltaTime * speed + Vector3.right * horizontalVelocity*Time.fixedDeltaTime* speed,Space.Self);
        //Vector3 newPosition = transform.position + Vector3.forward * upwardsVelocity*Time.fixedDeltaTime * speed + Vector3.right * horizontalVelocity*Time.fixedDeltaTime* speed;
        //transform.position = newPosition;
        //El cliente no puede escribir en las networks variables, son readonly, solo el servidor puede escribir en ellas
        //Position.Value = transform.position;
        //Vector3 newPosition = transform.position + new Vector3(upwardsVelocity*speed,0,horizontalVelocity*speed);
        //SubmitPositionRequestServerRpc(newPosition);

        //float moveX = Input.GetAxis("Horizontal");
        //float moveZ = Input.GetAxis("Vertical");

        //Vector3 newPosition = new Vector3(moveX,0,moveZ);
        //transform.position += newPosition * speed * Time.deltaTime;

        //transform.Translate(Vector3.forward * moveX*Time.fixedDeltaTime * speed + Vector3.right * moveZ*Time.fixedDeltaTime* speed,Space.Self);
        SubmitPositionRequestServerRpc(upwardsVelocity,horizontalVelocity);
    
    }

    [Rpc(SendTo.Server)]
    void SubmitPositionRequestServerRpc(float moveX,float moveZ)
    {
        //Position.Value = newPosition;
        transform.Translate(Vector3.forward * moveX*Time.fixedDeltaTime * speed + Vector3.right * moveZ*Time.fixedDeltaTime* speed,Space.Self);
    }

    void FixedUpdate()
    {
        if (IsOwner){
            Move();
        }
    }
}
