// BRAXHORN CITY: import original estate into an ISOLATED Unity 6 scene.
// Exact source model placements, no replacement primitives, no changes to other scenes.
#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
namespace Braxhorn.EstateMigration {
public static class BraxEstateBuild {
 const string Root="Assets/EstateMigration";
 const string ManifestPath=Root+"/Generated/EstateSourceManifest.json";
 const string WorldPath=Root+"/Generated/EstateProceduralAndDoors.glb";
 const string ModelDir=Root+"/Source";
 [Serializable] public class FileInfoEntry {
  public string name,source,sha256;public bool present;public long bytes;
 }
 [Serializable] public class ModelData {
  public string name,source;public float x,z,width,rotation;
 }
 [Serializable] public class BoxData {
  public float minX,maxX,minY,maxY,minZ,maxZ;
 }
 [Serializable] public class GroundData {
  public float x0,x1,z0,z1,x,z,y,radius;
 }
 [Serializable] public class DoorData {
  public string id,label;public float x,z,yaw,width,height,@base;
  public bool single,glass,wood;public int leafCount,initialSwing;
 }
 [Serializable] public class PlantData {
  public string kind;public float x,z,height,rotation;
 }
 [Serializable] public class LayoutData { public string id,version;public float[] spawn; }
 [Serializable] public class EstateManifest {
  public string schema,status,generatedGlbSha256;
  public FileInfoEntry[] binaryAssets;public ModelData[] sourceModels;
  public BoxData[] collision;public GroundData[] walkable;public DoorData[] doors;
  public PlantData[] vegetation;public string[] missingOriginalModels;
  public LayoutData level;
 }
 static string Repo(string relative) => Path.GetFullPath(Path.Combine(Application.dataPath,"..","..",relative.Replace('/',Path.DirectorySeparatorChar)));
 static string AssetFile(string assetPath) => Path.GetFullPath(Path.Combine(Application.dataPath,"..",assetPath.Replace('/',Path.DirectorySeparatorChar)));
 static void Folder(string name) {Directory.CreateDirectory(AssetFile(name));}
 static string Sha(string file) {
  using(var sha=SHA256.Create())using(var fs=File.OpenRead(file))
   return BitConverter.ToString(sha.ComputeHash(fs)).Replace("-","").ToLowerInvariant();
 }
 static GameObject Import(string original,string assetPath,string expected=null) {
  if(!File.Exists(original))throw new FileNotFoundException("Original GLB missing: "+original);
  if(expected!=null&&!Sha(original).Equals(expected,StringComparison.OrdinalIgnoreCase))
   throw new InvalidDataException("SOURCE SHA256 CHANGED: "+original);
  string to=AssetFile(assetPath);
  Folder(Path.GetDirectoryName(assetPath).Replace('\\','/'));
  if(!Path.GetFullPath(original).Equals(Path.GetFullPath(to),StringComparison.OrdinalIgnoreCase)
      &&(!File.Exists(to)||Sha(to)!=Sha(original))){
   File.Copy(original,to,true);
   AssetDatabase.ImportAsset(assetPath,ImportAssetOptions.ForceSynchronousImport|ImportAssetOptions.ForceUpdate);
  }
  GameObject prefab=AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
  if(prefab==null){
   AssetDatabase.ImportAsset(assetPath,ImportAssetOptions.ForceSynchronousImport|ImportAssetOptions.ForceUpdate);
   prefab=AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
  }
  if(prefab==null)throw new InvalidDataException("glTFast failed to import ORIGINAL asset "+assetPath);
  return prefab;
 }
 static FileInfoEntry Original(EstateManifest m,string name) =>
  m.binaryAssets.FirstOrDefault(x=>x.name==name&&x.present&&!string.IsNullOrEmpty(x.source));
 static GameObject Prefab(EstateManifest m,string name,Dictionary<string,GameObject> cache) {
  if(cache.TryGetValue(name,out var hit))return hit;
  var rec=Original(m,name);
  if(rec==null)throw new FileNotFoundException("Required original model is not in the repository: "+name);
  hit=Import(Repo(rec.source),ModelDir+"/"+name+".glb",rec.sha256);
  cache.Add(name,hit);return hit;
 }
 static GameObject Instance(GameObject prefab,Transform parent,string name) {
  var go=PrefabUtility.InstantiatePrefab(prefab) as GameObject;
  if(go==null)throw new InvalidOperationException("Cannot instantiate original GLB "+name);
  go.name=name;go.transform.SetParent(parent,false);return go;
 }
 static Bounds VisibleBounds(GameObject go) {
  var renderers=go.GetComponentsInChildren<Renderer>(true);
  if(renderers.Length==0)throw new InvalidDataException("No original geometry: "+go.name);
  var b=renderers[0].bounds;
  foreach(var r in renderers.Skip(1))b.Encapsulate(r.bounds);
  return b;
 }
 static void MeshColliders(GameObject root) {
  foreach(var f in root.GetComponentsInChildren<MeshFilter>(true)){
   if(f.sharedMesh==null||f.sharedMesh.vertexCount<3)continue;
   var mesh=f.GetComponent<MeshCollider>()??f.gameObject.AddComponent<MeshCollider>();
   mesh.sharedMesh=f.sharedMesh;mesh.convex=false;
  }
 }
 static Bounds ThreeAabb(float x0,float x1,float y0,float y1,float z0,float z1) {
  var b=new Bounds();
  b.SetMinMax(new Vector3(x0,y0,-z1),new Vector3(x1,y1,-z0));return b;
 }
 static Bounds? Passage(string name) {
  if(name=="gameready_casino_scene")return ThreeAabb(-90.7f,-88.1f,.15f,3.3f,-36.2f,-34.5f);
  if(name=="nightclub")return ThreeAabb(-96,-76,-1,8,15.8f,30);
  if(name=="policestation")return ThreeAabb(84.7f,87.3f,.12f,2.7f,-34.3f,-32.4f);
  return null;
 }
 static void PoliceProps(GameObject root) {
  // Original level.js source offsets these small prop clusters by (-12,0,+5).
  foreach(var f in root.GetComponentsInChildren<MeshFilter>(true)){
   var r=f.GetComponent<Renderer>();if(r==null)continue;
   var b=r.bounds,s=b.size;
   float sourceMinZ=-b.max.z,sourceMaxZ=-b.min.z;
   if(s.x<2.2f&&s.z<2.2f&&s.y<1.6f&&b.max.y<1.7f&&
    b.max.x>85.2f&&b.min.x<86.8f&&sourceMaxZ>-37.2f&&sourceMinZ<-34.1f)
    f.transform.position+=new Vector3(-12,0,-5);
  }
 }
 static void AddModel(EstateManifest m,ModelData p,Dictionary<string,GameObject> cache,Transform parent,int i) {
  GameObject go=Instance(Prefab(m,p.name,cache),parent,"Original_"+i.ToString("00")+"_"+p.name);
  // Three.js level.js: rotate model, normalize to target WORLD width, recenter Box3, minY=.08.
  // glTFast flips the handedness, therefore Three (x,y,z) => Unity (x,y,-z).
  go.transform.position=Vector3.zero;
  go.transform.rotation=Quaternion.Euler(0,-p.rotation*Mathf.Rad2Deg,0);
  go.transform.localScale=Vector3.one;
  Bounds b=VisibleBounds(go);
  if(b.size.x<.0001f)throw new InvalidDataException("Cannot normalize original GLB: "+p.name);
  go.transform.localScale=Vector3.one*(p.width/b.size.x);
  b=VisibleBounds(go);
  go.transform.position=new Vector3(p.x-b.center.x,.08f-b.min.y,-p.z-b.center.z);
  if(p.name=="policestation")PoliceProps(go);
  Bounds? cut=Passage(p.name);
  if(cut.HasValue&&BraxEstateMeshCut.RemoveBox(go,cut.Value)==0)
   throw new InvalidDataException("Original entrance cut failed for "+p.name);
  MeshColliders(go);
 }
 static void Physics(EstateManifest m,Transform parent) {
  var collision=new GameObject("ORIGINAL_PROCEDURAL_COLLISIONS");
  collision.transform.SetParent(parent,false);
  for(int i=0;i<m.collision.Length;i++){
   var b=m.collision[i];
   var go=new GameObject("box_"+i.ToString("0000"));go.transform.SetParent(collision.transform,false);
   var box=go.AddComponent<BoxCollider>();
   box.size=new Vector3(Mathf.Max(.015f,b.maxX-b.minX),
    Mathf.Max(.015f,b.maxY-b.minY),Mathf.Max(.015f,b.maxZ-b.minZ));
   go.transform.position=new Vector3((b.minX+b.maxX)/2,(b.minY+b.maxY)/2,-(b.minZ+b.maxZ)/2);
  }
  for(int i=0;i<m.walkable.Length;i++){
   var w=m.walkable[i];var go=new GameObject("walkable_"+i.ToString("000"));
   go.transform.SetParent(collision.transform,false);
   float x0=w.radius>0?w.x-w.radius:w.x0,x1=w.radius>0?w.x+w.radius:w.x1;
   float z0=w.radius>0?w.z-w.radius:w.z0,z1=w.radius>0?w.z+w.radius:w.z1;
   var box=go.AddComponent<BoxCollider>();
   box.size=new Vector3(Mathf.Max(.1f,x1-x0),.10f,Mathf.Max(.1f,z1-z0));
   go.transform.position=new Vector3((x0+x1)/2,w.y-.05f,-(z0+z1)/2);
  }
 }
 static void Plants(EstateManifest m,Dictionary<string,GameObject> cache,Transform parent,bool survey) {
  var root=new GameObject("ORIGINAL_VEGETATION_30");root.transform.SetParent(parent,false);
  for(int i=0;i<m.vegetation.Length;i++){
   var p=m.vegetation[i];string name="park_"+p.kind;
   if(Original(m,name)==null){if(survey)continue;throw new FileNotFoundException("Missing vegetation source "+name);}
   var pivot=new GameObject("SOURCE_PLANT_"+i+"_"+p.kind);
   pivot.transform.SetParent(root.transform,false);
   var model=Instance(Prefab(m,name,cache),pivot.transform,"ORIGINAL_"+name);
   model.transform.localPosition=Vector3.zero;
   model.transform.localScale=Vector3.one;
   var b=VisibleBounds(model);var anchor=b.center;anchor.y=b.min.y;
   if(p.kind=="tree"){
    var named=model.GetComponentsInChildren<Renderer>(true).Where(x=>x.name.Contains("jakaranda")).ToArray();
    if(named.Length>0){
     var tr=named[0].bounds;foreach(var r in named.Skip(1))tr.Encapsulate(r.bounds);
     anchor.x=tr.center.x;anchor.z=tr.center.z;
    }
   }
   model.transform.localPosition=-anchor;
   pivot.transform.localScale=Vector3.one*(p.height/Mathf.Max(.001f,b.size.y));
   pivot.transform.rotation=Quaternion.Euler(0,-p.rotation*Mathf.Rad2Deg,0);
   pivot.transform.position=new Vector3(p.x,.07f,-p.z);
   float radius=p.kind=="tree"?.22f:p.kind=="rock"?p.height*.75f:0;
   if(radius>0){
    // Collision independent of visual scale (as in original vegetation.js).
    var go=new GameObject("ORIGINAL_TRUNK_OR_ROCK_COLLIDER");
    go.transform.SetParent(root.transform,false);
    go.transform.position=new Vector3(p.x,0,-p.z);
    var c=go.AddComponent<BoxCollider>();
    float h=p.kind=="tree"?3:p.height;
    c.center=new Vector3(0,h/2,0);c.size=new Vector3(radius*2,h,radius*2);
   }
  }
 }
 static void ConnectDoors(EstateManifest m,GameObject world) {
  var nodes=world.GetComponentsInChildren<Transform>(true);
  int count=0;
  foreach(var d in m.doors){
   var root=nodes.FirstOrDefault(x=>x.name=="door-"+d.id);
   if(root==null)throw new InvalidDataException("Original door node missing: "+d.id);
   var hinges=root.GetComponentsInChildren<Transform>(true)
    .Where(x=>x.name.StartsWith("HINGE_"+d.id+"_")).OrderBy(x=>x.name).ToArray();
   if(hinges.Length!=d.leafCount)throw new InvalidDataException("Door leaf mismatch: "+d.id);
   var door=root.gameObject.AddComponent<BraxEstateDoor>();
   door.Initialize(d.id,d.label,d.width,d.single,hinges);
   foreach(var hinge in hinges)
   foreach(var f in hinge.GetComponentsInChildren<MeshFilter>(true)){
    if(f.sharedMesh==null)continue;
    var collider=f.gameObject.AddComponent<BoxCollider>();
    collider.center=f.sharedMesh.bounds.center;
    collider.size=f.sharedMesh.bounds.size;
   }
   count++;
  }
  if(count!=24)throw new InvalidDataException("Must preserve all 24 source doors.");
  var frames=nodes.FirstOrDefault(x=>x.name=="estate-door-frames");
  if(frames==null)throw new InvalidDataException("Original source door frames missing");
  MeshColliders(frames.gameObject);
 }
 static void Brax(Transform parent) {
  string compressed=Repo("public/assets/chars/brax.glb.gz");
  if(!File.Exists(compressed))throw new FileNotFoundException("Original Brax is mandatory");
  string path=ModelDir+"/brax-original.glb",dest=AssetFile(path);
  Folder(ModelDir);
  if(!File.Exists(dest)){
   using(var input=File.OpenRead(compressed))
   using(var gzip=new System.IO.Compression.GZipStream(input,System.IO.Compression.CompressionMode.Decompress))
   using(var output=File.Create(dest))gzip.CopyTo(output);
  }
  var prefab=Import(dest,path);
  var player=new GameObject("BRAX_ORIGINAL_SURVEY_PLAYER");player.transform.SetParent(parent,false);
  player.transform.position=new Vector3(0,.08f,-92); // default dev/osiedle/main.js boot entry
  var cc=player.AddComponent<CharacterController>();
  cc.height=2.2f;cc.radius=.6f;cc.center=new Vector3(0,1.1f,0);cc.stepOffset=.3f;
  var rig=Instance(prefab,player.transform,"SOURCE_BRAX_RIG");
  rig.transform.localPosition=Vector3.zero;
  rig.transform.localScale=Vector3.one*.024f; // exact dev/src/fighter.js rig scale
  var controller=player.AddComponent<BraxEstateWalker>();controller.model=rig.transform;
  var cameraObject=new GameObject("SOURCE_PLAYER_CAMERA");
  cameraObject.transform.SetParent(player.transform,false);
  var camera=cameraObject.AddComponent<Camera>();
  camera.fieldOfView=55;camera.nearClipPlane=.08f;camera.farClipPlane=400;
  cameraObject.AddComponent<AudioListener>();controller.cam=camera;
  var clips=AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().ToArray();
  var idle=clips.FirstOrDefault(x=>x.name=="Idle");
  var walk=clips.FirstOrDefault(x=>x.name=="Walking");
  var run=clips.FirstOrDefault(x=>x.name=="Running");
  if(idle==null||walk==null||run==null) {
   Debug.LogWarning("SOURCE Brax animation clips missing from imported GLB. No fake gait animation installed. ANIMATION QA BLOCKED.");
   return;
  }
  Folder(Root+"/Animations");
  string animPath=Root+"/Animations/BraxSourceEstate.controller";
  var ctrl=AssetDatabase.LoadAssetAtPath<AnimatorController>(animPath);
  if(ctrl==null){
   ctrl=AnimatorController.CreateAnimatorControllerAtPath(animPath);
   ctrl.AddParameter("Speed",AnimatorControllerParameterType.Float);
   var sm=ctrl.layers[0].stateMachine;
   var sIdle=sm.AddState("SourceIdle");sIdle.motion=idle;sm.defaultState=sIdle;
   var sWalk=sm.AddState("SourceWalk");sWalk.motion=walk;
   var sRun=sm.AddState("SourceRun");sRun.motion=run;
   Link(sIdle,sWalk,AnimatorConditionMode.Greater,.11f);
   Link(sWalk,sIdle,AnimatorConditionMode.Less,.10f);
   Link(sWalk,sRun,AnimatorConditionMode.Greater,.75f);
   Link(sRun,sWalk,AnimatorConditionMode.Less,.70f);
   Link(sIdle,sRun,AnimatorConditionMode.Greater,.75f);
   Link(sRun,sIdle,AnimatorConditionMode.Less,.10f);
  }
  var animator=rig.GetComponent<Animator>()??rig.AddComponent<Animator>();
  animator.runtimeAnimatorController=ctrl;animator.applyRootMotion=false;
  controller.animator=animator;
 }
 static void Link(AnimatorState a,AnimatorState b,AnimatorConditionMode mode,float threshold) {
  var t=a.AddTransition(b);t.hasExitTime=false;t.duration=.15f;
  t.AddCondition(mode,threshold,"Speed");
 }
 static void Lighting() {
  RenderSettings.ambientMode=AmbientMode.Trilight;
  RenderSettings.ambientSkyColor=new Color(.80f,.85f,.83f);
  RenderSettings.ambientEquatorColor=new Color(.65f,.70f,.67f);
  RenderSettings.ambientGroundColor=new Color(.51f,.47f,.40f);
  RenderSettings.fog=true;
  RenderSettings.fogColor=new Color(.741f,.8f,.816f);
  RenderSettings.fogMode=FogMode.Linear;
  RenderSettings.fogStartDistance=160;
  RenderSettings.fogEndDistance=330;
  var sun=new GameObject("ORIGINAL_DAY_SOURCE_LIGHT").AddComponent<Light>();
  sun.type=LightType.Directional;sun.color=new Color(1,.94f,.83f);
  sun.intensity=2.0f;sun.shadows=LightShadows.Soft;
  sun.transform.rotation=Quaternion.LookRotation(new Vector3(55,-110,-45).normalized);
 }
 [MenuItem("BRAXHORN/Osiedle/Import 1:1 ONLY if all assets exist")]
 public static void BuildStrict()=>Build(false);
 [MenuItem("BRAXHORN/Osiedle/Partial survey - NOT 1:1")]
 public static void BuildSurvey()=>Build(true);
 static void Build(bool survey){
  try{BuildCore(survey);}
  catch(Exception e){Debug.LogError("Estate Unity import not accepted. "+e);
   EditorUtility.DisplayDialog("BRAXHORN City / osiedle",e.Message+"\nBrak podmian i brak publikacji.","OK");}
 }
 static void BuildCore(bool survey){
  if(!File.Exists(AssetFile(ManifestPath)))throw new FileNotFoundException("First run source exporter; manifest not found.");
  var m=JsonUtility.FromJson<EstateManifest>(File.ReadAllText(AssetFile(ManifestPath)));
  if(m==null||m.schema!="braxhorn-estate-source-1"||m.sourceModels?.Length!=21||
    m.doors?.Length!=24||m.vegetation?.Length!=30||m.collision?.Length==0)
    throw new InvalidDataException("Exact source inventory invalid; do not build a fake 1:1.");
  if(!File.Exists(AssetFile(WorldPath))||Sha(AssetFile(WorldPath))!=m.generatedGlbSha256)
    throw new InvalidDataException("Exported procedural GLB missing or SHA mismatch.");
  var missing=m.binaryAssets.Where(x=>!x.present||string.IsNullOrEmpty(x.source)||
    !File.Exists(Repo(x.source))).Select(x=>x.name).ToArray();
  if(missing.Length>0&&!survey)throw new FileNotFoundException(
    "Missing original GLBs: "+string.Join(", ",missing)+". 1:1 import BLOCKED.");
  if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
  Folder(Root+"/Scenes");Folder(ModelDir);
  var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
  var root=new GameObject(survey?"ESTATE_PARTIAL_SURVEY_NOT_ACCEPTED":"ESTATE_ORIGINAL_SOURCE_LOCK");
  var prefabCache=new Dictionary<string,GameObject>();
  var world=Instance(Import(AssetFile(WorldPath),WorldPath,m.generatedGlbSha256),root.transform,"ORIGINAL_PROCEDURAL_WORLD_DOORS");
  var sourceModels=new GameObject("ORIGINAL_21_MODEL_INSTANCES");
  sourceModels.transform.SetParent(root.transform,false);
  int imported=0;
  for(int i=0;i<m.sourceModels.Length;i++){
   if(Original(m,m.sourceModels[i].name)==null&&survey)continue;
   AddModel(m,m.sourceModels[i],prefabCache,sourceModels.transform,i);imported++;
  }
  Plants(m,prefabCache,root.transform,survey);
  Physics(m,root.transform);
  ConnectDoors(m,world);
  Brax(root.transform);
  Lighting();
  var dest=Root+"/Scenes/"+(survey?"EstatePartialSurvey_NOT_1TO1":"EstateOriginal_1TO1_UNVERIFIED")+".unity";
  EditorSceneManager.MarkSceneDirty(scene);
  if(!EditorSceneManager.SaveScene(scene,dest))throw new IOException("Scene could not be saved.");
  AssetDatabase.SaveAssets();
  Debug.Log("Estate Unity scene generated on isolated branch. Mode="+(survey?"SURVEY":"STRICT")+
   ", model instances="+imported+"/21, source door objects=24, colliders="+m.collision.Length+
   ", vegetation placements="+m.vegetation.Length+". UNITY COMPILE/VISUAL/MOBILE QA NOT ACCEPTED.");
  EditorUtility.DisplayDialog("BRAXHORN City / import",
   "Scena źródłowa zapisana: "+dest+"\nOdbiór 1:1: NIEPOTWIERDZONY. Przejdź drzwi i wnętrza w Unity i porównaj obrazy.", "OK");
 }
}
}
#endif
