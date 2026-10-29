// Temporary control for the ORIGINAL Brax rig: no surrogate avatar or animations.
using UnityEngine;
namespace Braxhorn.EstateMigration {
[RequireComponent(typeof(CharacterController))]
public class BraxEstateWalker:MonoBehaviour {
 public Transform model;public Animator animator;public Camera cam;
 public float yaw,pitch=.30f,distance=5.8f;
 CharacterController cc;float gravity=-.2f;
 int joyId=-1,lookId=-1;Vector2 joyStart,joyNow;
 void Awake(){cc=GetComponent<CharacterController>();}
 void Update(){
  float x=0,z=0;bool movingOnTouch=Application.isMobilePlatform;
  if(movingOnTouch){
   foreach(var t in Input.touches){
    if(t.phase==TouchPhase.Began){
     if(t.position.x<Screen.width*.44f&&joyId<0){joyId=t.fingerId;joyStart=joyNow=t.position;}
     else if(lookId<0)lookId=t.fingerId;
    }
    if(t.fingerId==joyId){joyNow=t.position;if(t.phase==TouchPhase.Ended||t.phase==TouchPhase.Canceled)joyId=-1;}
    if(t.fingerId==lookId){
     if(t.phase==TouchPhase.Moved){yaw-=t.deltaPosition.x*.32f;pitch=Mathf.Clamp(pitch+t.deltaPosition.y*.004f,.05f,1.2f);}
     if(t.phase==TouchPhase.Ended||t.phase==TouchPhase.Canceled)lookId=-1;
    }
   }
   if(joyId>=0){var v=Vector2.ClampMagnitude((joyNow-joyStart)/(Mathf.Min(Screen.width,Screen.height)*.16f),1);x=v.x;z=v.y;}
  }else{
   x=(Input.GetKey(KeyCode.D)||Input.GetKey(KeyCode.RightArrow)?1:0)-(Input.GetKey(KeyCode.A)||Input.GetKey(KeyCode.LeftArrow)?1:0);
   z=(Input.GetKey(KeyCode.W)||Input.GetKey(KeyCode.UpArrow)?1:0)-(Input.GetKey(KeyCode.S)||Input.GetKey(KeyCode.DownArrow)?1:0);
   if(Input.GetMouseButton(1)){yaw-=Input.GetAxis("Mouse X")*3f;pitch=Mathf.Clamp(pitch+Input.GetAxis("Mouse Y")*.04f,.05f,1.2f);}
  }
  var axis=Vector2.ClampMagnitude(new Vector2(x,z),1);
  var rad=yaw*Mathf.Deg2Rad;
  var forward=new Vector3(Mathf.Sin(rad),0,Mathf.Cos(rad));
  var right=new Vector3(Mathf.Cos(rad),0,-Mathf.Sin(rad));
  var movement=(forward*axis.y+right*axis.x);
  bool run=movingOnTouch||Input.GetKey(KeyCode.LeftShift);
  float speed=run?5:2; // Source dev/src/fighter.js
  gravity=cc.isGrounded?-.2f:gravity-18*Time.deltaTime;
  cc.Move((movement*speed+Vector3.up*gravity)*Time.deltaTime);
  if(model!=null&&movement.sqrMagnitude>.01f)
   model.rotation=Quaternion.Slerp(model.rotation,Quaternion.LookRotation(movement),Time.deltaTime*9);
  if(animator!=null)animator.SetFloat("Speed",axis.magnitude*(run?1:.5f),.12f,Time.deltaTime);
  if(Input.GetKeyDown(KeyCode.E))OpenDoor();
 }
 void OpenDoor(){
  BraxEstateDoor closest=null;float best=float.PositiveInfinity;
  foreach(var d in FindObjectsByType<BraxEstateDoor>(FindObjectsSortMode.None)){
   float dist=Vector3.Distance(transform.position,d.transform.position);
   if(dist<best&&d.InRange(transform.position)){best=dist;closest=d;}
  }
  closest?.Use(transform.position);
 }
 void LateUpdate(){
  if(cam==null)return;
  var target=transform.position+Vector3.up*1.6f;
  float a=yaw*Mathf.Deg2Rad;
  Vector3 offset=new Vector3(Mathf.Sin(a)*Mathf.Cos(pitch),Mathf.Sin(pitch),-Mathf.Cos(a)*Mathf.Cos(pitch))*distance;
  Vector3 destination=target+offset;Vector3 ray=destination-target;
  if(Physics.SphereCast(target,.22f,ray.normalized,out RaycastHit hit,distance,~0,QueryTriggerInteraction.Ignore)
      &&hit.collider.gameObject!=gameObject)
   destination=target+ray.normalized*Mathf.Max(.75f,hit.distance-.3f);
  cam.transform.position=Vector3.Lerp(cam.transform.position,destination,1-Mathf.Exp(-16*Time.deltaTime));
  cam.transform.LookAt(target);
 }
 void OnGUI(){
  if(!Application.isMobilePlatform)return;
  if(GUI.Button(new Rect(Screen.width-140,Screen.height-100,120,70),"OTWÓRZ"))OpenDoor();
  if(joyId>=0)GUI.Box(new Rect(joyStart.x-48,Screen.height-joyStart.y-48,96,96),"Ruch");
 }
}
}
