// Triangle clipping equivalent to dev/osiedle/doors.js, not a rebuilt facade.
#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.Rendering;
using System;
using System.Collections.Generic;
namespace Braxhorn.EstateMigration {
internal static class BraxEstateMeshCut {
 struct Vertex {
  public Vector3 pos,world,normal;public Vector2 uv;public Vector4 tan;public Color color;
  public static Vertex Lerp(Vertex a,Vertex b,float t) {
   return new Vertex {pos=Vector3.LerpUnclamped(a.pos,b.pos,t),world=Vector3.LerpUnclamped(a.world,b.world,t),
    normal=Vector3.LerpUnclamped(a.normal,b.normal,t),uv=Vector2.LerpUnclamped(a.uv,b.uv,t),
    tan=Vector4.LerpUnclamped(a.tan,b.tan,t),color=Color.LerpUnclamped(a.color,b.color,t)};
  }
 }
 struct Plane {public int axis;public float at,sign; public Plane(int a,float p,float s){axis=a;at=p;sign=s;}}
 static float Coordinate(Vector3 p,int axis)=>axis==0?p.x:axis==1?p.y:p.z;
 static void Split(List<Vertex> input,Plane plane,out List<Vertex> inside,out List<Vertex> outside){
  inside=new List<Vertex>();outside=new List<Vertex>();
  for(int i=0;i<input.Count;i++){
   Vertex a=input[i],b=input[(i+1)%input.Count];
   float da=(Coordinate(a.world,plane.axis)-plane.at)*plane.sign;
   float db=(Coordinate(b.world,plane.axis)-plane.at)*plane.sign;
   (da<=0?inside:outside).Add(a);
   if((da<0&&db>0)||(da>0&&db<0)){var p=Vertex.Lerp(a,b,da/(da-db));inside.Add(p);outside.Add(p);}
  }
 }
 static void Emit(List<Vertex> poly,List<int> tri,List<Vector3> pos,List<Vector2> uv,
  List<Vector3> norm,List<Vector4> tan,List<Color> color){
  for(int i=1;i<poly.Count-1;i++)foreach(var v in new[]{poly[0],poly[i],poly[i+1]}){
   tri.Add(pos.Count);pos.Add(v.pos);uv.Add(v.uv);norm.Add(v.normal.normalized);
   tan.Add(v.tan);color.Add(v.color);
  }
 }
 public static int RemoveBox(GameObject root,Bounds cut){
  var lo=cut.min;var hi=cut.max;
  var planes=new[]{new Plane(0,lo.x,-1),new Plane(0,hi.x,1),new Plane(1,lo.y,-1),
    new Plane(1,hi.y,1),new Plane(2,lo.z,-1),new Plane(2,hi.z,1)};
  int changed=0;
  foreach(var f in root.GetComponentsInChildren<MeshFilter>(true)){
   if(f.sharedMesh==null)continue;
   var renderer=f.GetComponent<Renderer>();
   if(renderer==null||!renderer.bounds.Intersects(cut))continue;
   Mesh source=f.sharedMesh;
   var p0=source.vertices;var n0=source.normals;var uv0=source.uv;
   var t0=source.tangents;var c0=source.colors;
   var verts=new List<Vector3>();var norms=new List<Vector3>();
   var uvs=new List<Vector2>();var tangents=new List<Vector4>();var colors=new List<Color>();
   var tris=new List<int>[source.subMeshCount];
   for(int sm=0;sm<source.subMeshCount;sm++){
    tris[sm]=new List<int>();
    var idx=source.GetTriangles(sm);
    for(int i=0;i+2<idx.Length;i+=3){
     var poly=new List<Vertex>(3);
     for(int j=0;j<3;j++){
      int n=idx[i+j];poly.Add(new Vertex{
       pos=p0[n],world=f.transform.TransformPoint(p0[n]),normal=n0.Length==p0.Length?n0[n]:Vector3.up,
       uv=uv0.Length==p0.Length?uv0[n]:Vector2.zero,
       tan=t0.Length==p0.Length?t0[n]:new Vector4(1,0,0,1),
       color=c0.Length==p0.Length?c0[n]:Color.white
      });
     }
     foreach(var plane in planes){
      if(poly.Count<3)break;
      Split(poly,plane,out var keepInside,out var keepOutside);
      Emit(keepOutside,tris[sm],verts,uvs,norms,tangents,colors);
      poly=keepInside;
     } // final inside polygon is deleted
    }
   }
   var dest=new Mesh{ name=source.name+"_opening", indexFormat=IndexFormat.UInt32};
   dest.SetVertices(verts);dest.SetUVs(0,uvs);dest.SetNormals(norms);
   dest.SetTangents(tangents);dest.SetColors(colors);
   dest.subMeshCount=source.subMeshCount;
   for(int sm=0;sm<tris.Length;sm++)dest.SetTriangles(tris[sm],sm,false);
   dest.RecalculateBounds();f.sharedMesh=dest;changed++;
  }
  return changed;
 }
}
}
#endif
