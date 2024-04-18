using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;


[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]
public class Player_Transformless : NetworkBehaviour
{
    //Necesitamos una variable de autoridad en cliente (que no se puede editar en servidor) y una de autoridad en servidor (que no se puede editar en cliente)
    public NetworkVariable<Vector3> ImprovisedTransformServerAutority = new NetworkVariable<Vector3>();
    public NetworkVariable<Vector3> ImprovisedTransformClientAutority = new NetworkVariable<Vector3>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    // Start is called before the first frame update
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
        //Debug.Log("{CLIENT} Spawned");
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
            //transform.position = newPosition;
            
            ImprovisedTransformClientAutority.Value = newPosition;
            transform.position = ImprovisedTransformClientAutority.Value;
            
            
            SubmitNewPositionRpc(newPosition);
        } else if (PlayMode.Value == 2){
            Vector3 newPosition = transform.position;
            newPosition.x = transform.position.x + xVelocity*Time.fixedDeltaTime* speed;
            newPosition.z = transform.position.z + zVelocity*Time.fixedDeltaTime*speed;
            ImprovisedTransformClientAutority.Value = newPosition;
            transform.position = ImprovisedTransformClientAutority.Value;
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
        ImprovisedTransformServerAutority.Value = newPosition;
        //transform.position = ImprovisedTransform.Value;
    }
    [Rpc(SendTo.Server)]
    void SubmitNewPositionRpc(Vector3 newPosition)
    {
        ImprovisedTransformServerAutority.Value = newPosition;
        //transform.position = newPosition;
        if (newPosition.x >= 5){
            //transform.position = Vector3.up;
            ImprovisedTransformServerAutority.Value = new Vector3(transform.position.x-0.1f,transform.position.y,transform.position.z);
        } else if (newPosition.z >= 5){
            ImprovisedTransformServerAutority.Value = new Vector3(transform.position.x,transform.position.y,transform.position.z-0.1f);
        } else if (newPosition.x <= -5){
            ImprovisedTransformServerAutority.Value = new Vector3(transform.position.x+0.1f,transform.position.y,transform.position.z);
        } else if (newPosition.z <= -5){
            ImprovisedTransformServerAutority.Value = new Vector3(transform.position.x,transform.position.y,transform.position.z+0.1f);
        }
        //cambiar variables en funcion como si fuese asyncrona
        //ServerResponse(ImprovisedTransformServerAutority.Value);
    }
    void ServerResponse(Vector3 newPosition){
        ImprovisedTransformClientAutority.Value = newPosition;
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
            Movement();
        }
    }
    void Update(){
            switch (PlayMode.Value){
                case 1:
                //ServerAutority
                transform.position = ImprovisedTransformServerAutority.Value;
                //Hay problemas con el seteo de NetworksVariables entre si, por lo que tenemos que setearlo a nuestra position
                ImprovisedTransformClientAutority.Value = ImprovisedTransformServerAutority.Value;
                //ImprovisedTransformClientAutority.Value = transform.position;
                break;
                case 2:
                //ClientAutority
                transform.position = ImprovisedTransformClientAutority.Value;
                //Seteamos las posiciones de servidor y cliente para evitar teletransportes
                //Tenemos un problema de invalid operation, ya que estamos sobreescribiendo una variable con permisos de escritura en el servidor
                //Pero por alguna razon que escapa totalmente a mi comprension, funciona igualmente
                ImprovisedTransformServerAutority.Value = ImprovisedTransformClientAutority.Value;
                break;
                case 3:
                //ServerAutorityRewind
                //Debugs para mostrar su correcto funcionamiento
                Debug.Log("{SERVER} Vector3("+ImprovisedTransformServerAutority.Value.x+","+ImprovisedTransformServerAutority.Value.y+","+ImprovisedTransformServerAutority.Value.z+")");
                Debug.Log("{CLIENT} Vector3("+ImprovisedTransformClientAutority.Value.x+","+ImprovisedTransformClientAutority.Value.y+","+ImprovisedTransformClientAutority.Value.z+")");
                ImprovisedTransformClientAutority.Value = ImprovisedTransformServerAutority.Value;
                transform.position = ImprovisedTransformServerAutority.Value;
                //ImprovisedTransformServerAutority.Value = ImprovisedTransformClientAutority.Value;
                break;
            }
        if (IsOwner){
            if (Input.GetKeyDown(KeyCode.Space)){
                Jump();
            }
        }
    }
}
