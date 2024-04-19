using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace Networking_Transformless{
    public class Optimized_PlayerTransformless : NetworkBehaviour
    {
        [Header("Movimiento del jugador")]
        public float speed;
        [Header("Referentes a la sincronizacion")]
        //Creamos una variable de autoridad en cliente
        private NetworkVariable<Vector3> PositionControlledByClient = new NetworkVariable<Vector3>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        //Creamos una variable gobernada por el servidor
        private NetworkVariable<Vector3> PositionControlledByServer = new NetworkVariable<Vector3>();
        //Creamos una variable para detectar cuando el servidor ha terminado su trabajo en el ServerRewind
        public NetworkVariable<bool> serverDoneNetwork = new NetworkVariable<bool>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        //Creamos una variable para detectar cuando es necesario la corrección de la posición en ServerRewind
        public NetworkVariable<bool> serverGetsAutority = new NetworkVariable<bool>();
        [Header("Referentes al movimiento")]
        private int zVelocity;
        private int xVelocity;
        public NetworkVariable<PLAYMODE> playmode= new NetworkVariable<PLAYMODE>();
        void Awake(){
            //subscribimos las NetworksVariables a los delegates OnValueChange, que son llamados al detectar un cambio de valor
            PositionControlledByClient.OnValueChanged += OnValueChangeClientVariable;
            PositionControlledByClient.OnValueChanged += OnValueChangeServerVariable;
        }
        //OnNetworkSpawn es llamada cada vez que se Spawnea Un Optimized_PlayerTransformless
        public override void OnNetworkSpawn()
        {
            if(IsOwner){
                //Al entrar en juego, debemos comprobar el tipo de control actual
                GetPlayModeRPC();
                //Cambiamos la posición inicial
                Invoke("SubmitInitialPositionRPC",0.3f); //Es necesario ponerle un pequeño delay para que no ignore la llamada (Imagino que es debido a una race condition relacionada con algo,no se ;-;)
            }
        }
        //Actualizamos nuestro modo de juego LA PRIMERA VEZ
        [Rpc(SendTo.Server)]
        void GetPlayModeRPC(){
            GameManager.instance.GetPlayMode(playmode);
        }
        //mandamos una señal desde el servidor para cambiar manualmente el modo de juego de todos los clientes
        public void ChangeAutority(PLAYMODE mode){
            SubmitNewAutorityRPC(mode);
        }
        //Actualizamos nuestro modo de juego
        [Rpc(SendTo.Server)]
        void SubmitNewAutorityRPC(PLAYMODE mode){
            playmode.Value = mode;
        }

        // Update is called once per frame
        void Update()
        {
            //Queremos que esta parte sea ejecutada por el owner
            if(IsOwner){
                //Programamos el movimiento
                Movement();
            }
            Debug.Log(playmode.Value);
            switch(playmode.Value){
                case PLAYMODE.ServerAutority:
                //Seteamos nuestra posición a la posición de la posición del servidor
                //es necesario hacerlo en el update porque con el OnValueChangeServerVariable no basta POR ALGUNA RAZON
                transform.position = PositionControlledByServer.Value;
                //Actualizamos la posicion controlada por el cliente para evitar teletransportes al cambiar el modo
                if(IsOwner)PositionControlledByClient.Value = transform.position; //Si no eres el propietario de la variable, no la puedes cambiar
                break;
                case PLAYMODE.ClientAutority:
                //En este caso, debemos setear la posiciones para que se sincronice en el resto de jugadores, pero nuestra posición local ya se ha cambiado al movernos
                transform.position = PositionControlledByClient.Value;
                //Al igual que antes, tenemos que sincronizar las variables de cada jugador para evitar teletransfortes,
                //pero como en este caso es la variable del servidor, debemos hacerlo desde el servidor
                if(IsOwner)SyncPositionRpc();
                break;
                case PLAYMODE.ServerRewind:
                //Esto se va a poner feo
                
                //La lógica es la que sigue: tenemos que actualizar nuestra posición a la posición gobernada
                //por el servidor, pero no podemos simplemente igualarla, ya que entonces estaríamos trabajando
                //con serverAutority pero menos optimizado

                //Si el servidor no ha terminado sus comprobaciones, no tenemos nada que hacer aqui
                Debug.Log("{PLAYMODE.ServerRewind}"+serverDoneNetwork.Value);
                if (!serverDoneNetwork.Value){
                    break;
                }
                //Actualizamos la posicion del servidor
                transform.position = PositionControlledByServer.Value;
                break;
            }
        }
        private void Movement(){
            zVelocity = Input.GetKey(KeyCode.W) ? 1 : Input.GetKey(KeyCode.S) ? -1 : 0;
            xVelocity = Input.GetKey(KeyCode.D) ? 1 : Input.GetKey(KeyCode.A) ? -1 : 0;
            switch(playmode.Value){
                case PLAYMODE.ServerAutority:
                //En este caso, solamente tenemos que cambiar la variable gobernada por el servidor,
                //por lo que debemos realizar un método rpc para que se ejecute en servidor
                //(ya que el cliente no tiene permisos de escritura en esta variable)
                SubmitPositionRequestServerRpc(zVelocity,xVelocity);
                break;
                case PLAYMODE.ClientAutority:
                //En este caso, solamente tenemos que cambiar la variable gobernada por el cliente,
                //por lo que no es necesario utilizar ningún método rpc,
                //de hecho esta variable no se puede cambiar en servidor al no tener permisos de escritura
                PositionControlledByClient.Value = MoveNewPosition();
                transform.position = PositionControlledByClient.Value;
                break;
                case PLAYMODE.ServerRewind:
                //Este es el peor caso de todos, ya que tenemos que cambiar ambas variables
                //la lógica es simple, primero cambiamos la variable gobernada por el cliente
                //y despues SI Y SOLO SI la nueva posición interfiere con los boundaries establecidos en servidor
                //cambiaremos la posición a una posición absoluta gobernada por la variable del servidor
                
                //Empezamos el trabajo en el servidor
                serverDoneNetwork.Value = false;
                //Nos movemos en local
                PositionControlledByClient.Value = MoveNewPosition();
                transform.position = PositionControlledByClient.Value;

                //Hacemos la comprobación en servidor
                SubmitPermitedPositionRpc(zVelocity,xVelocity);
                break;
            }
            
        }
        private Vector3 MoveNewPosition(){
            Vector3 newPosition = transform.position;
            newPosition.x = transform.position.x + xVelocity*Time.deltaTime* speed;
            newPosition.z = transform.position.z + zVelocity*Time.deltaTime*speed;
            return newPosition;
        }
        [Rpc(SendTo.Server)]
        private void SubmitPositionRequestServerRpc(float moveZ,float moveX){
            //Desgraciadamente, no podemos hacer un clamp de transform.Translate por como funciona, lo que significa que debemos cambiar la logica para utilizar newPosition
            Vector3 newPosition = transform.position;
            newPosition.x = Mathf.Clamp(transform.position.x + moveX*Time.deltaTime* speed,-5,5);
            newPosition.z = Mathf.Clamp(transform.position.z + moveZ*Time.deltaTime*speed,-5,5);
            PositionControlledByServer.Value = newPosition;
        }
        [Rpc(SendTo.Server)]
        private void SubmitPermitedPositionRpc(float moveZ,float moveX){
            //Al principio el servidor no tiene que actuar
            serverGetsAutority.Value = false;
            Vector3 newPosition = transform.position;
            newPosition.x = Mathf.Clamp(transform.position.x + moveX*Time.deltaTime* speed,-5.01f,5.01f);
            newPosition.z = Mathf.Clamp(transform.position.z + moveZ*Time.deltaTime*speed,-5.01f,5.01f);
            PositionControlledByServer.Value = newPosition;
            if (OutOfBoundaries(newPosition)){
                //si la posicion se sale de los limites, el servidor deberá actuar
                serverGetsAutority.Value = true;
            }
            if (!serverGetsAutority.Value) return; //Si el servidor no necesita actuar, no ejecutaremos nada
            
            //Al terminar, debemos setear la variable EXCLUSIVAMENTE en el cliente que ha empezado la acción, es decir, el owner
            ServerDoneRPC();
        }
        public bool OutOfBoundaries(Vector3 newPosition){
            if (newPosition.x >= 5){
                return true;
            } else if (newPosition.z >= 5){
                return true;
            } else if (newPosition.x <= -5){
                return true;
            } else if (newPosition.z <= -5){
                return true;
            }
            return false;
        }

        [Rpc(SendTo.Owner)]
        public void ServerDoneRPC(){
            serverDoneNetwork.Value = true;
        }

        [Rpc(SendTo.Server)]
        private void SubmitInitialPositionRPC(){
            //A veces esto se ejecuta 2 veces y a veces 0, unity es espectacular a veces
            if(playmode.Value == PLAYMODE.ServerAutority){
                PositionControlledByServer.Value = Vector3.up;
            }else{
                transform.position = Vector3.up;
            }
        }
        [Rpc(SendTo.Server)]
        private void SyncPositionRpc(){
            PositionControlledByServer.Value = transform.position;
        }

        public void OnValueChangeClientVariable(Vector3 previousValue, Vector3 newValue){
            //Si el cliente se ha salido de los limites, igualamos el valor de la variable cliente a la de servidor
            if (serverGetsAutority.Value){
                if (IsOwner)PositionControlledByClient.Value = PositionControlledByServer.Value;
                return;
            }
            //Actualizamos la posicion controlada por el cliente para evitar teletransportes al cambiar el modo
            transform.position = PositionControlledByClient.Value;
        }
        public void OnValueChangeServerVariable(Vector3 previousValue, Vector3 newValue){
            //Si el cliente no se ha salido de los limites, no hace nada
            if (!serverGetsAutority.Value){
                return;
            }
            transform.position = PositionControlledByServer.Value;
        }
    }
    public enum PLAYMODE{
        ServerAutority,
        ClientAutority,
        ServerRewind
    }
}
