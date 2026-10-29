// Retains source door mesh and hinge pivots from entrances.js.
using UnityEngine;
namespace Braxhorn.EstateMigration {
public class BraxEstateDoor:MonoBehaviour {
 public string sourceId,sourceLabel;public float width;public bool singleLeaf;
 public Transform[] hinges;public Quaternion[] rest;
 float progress,target,swing=1;
 public void Initialize(string id,string label,float w,bool single,Transform[] sourceHinges){
  sourceId=id;sourceLabel=label;width=w;singleLeaf=single;hinges=sourceHinges;
  rest=new Quaternion[hinges.Length];for(int i=0;i<hinges.Length;i++)rest[i]=hinges[i].localRotation;
 }
 void Awake(){
  if(rest!=null&&rest.Length==hinges.Length)return;
  rest=new Quaternion[hinges.Length];for(int i=0;i<hinges.Length;i++)rest[i]=hinges[i].localRotation;
 }
 public bool InRange(Vector3 pos){
  var p=transform.InverseTransformPoint(pos);
  return Mathf.Abs(p.x)<width*.5f+.3f&&Mathf.Abs(p.z)<2.1f&&Mathf.Abs(p.y)<.8f;
 }
 public void Use(Vector3 user){
  if(!InRange(user))return;
  if(target>.5f){if(Vector3.Distance(user,transform.position)<1f)return;target=0;}
  else{float z=transform.InverseTransformPoint(user).z;swing=singleLeaf&&z>0?-1:1;target=1;}
 }
 void Update(){
  if(hinges==null||rest==null||hinges.Length!=rest.Length)return;
  progress=Mathf.MoveTowards(progress,target,Time.deltaTime*1.3f);
  for(int i=0;i<hinges.Length;i++){
   var side=i==0?-1:1; // Original side -1 then +1.
   hinges[i].localRotation=rest[i]*Quaternion.Euler(0,side*progress*90*swing,0);
  }
 }
}
}
