using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player_Movement : MonoBehaviour
{
    protected int upwardsVelocity;
    protected int horizontalVelocity;
    protected float speed = 4;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
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
        transform.Translate(Vector3.forward * upwardsVelocity*Time.deltaTime * speed + Vector3.right * horizontalVelocity*Time.deltaTime* speed,Space.Self);
        
    }
}
