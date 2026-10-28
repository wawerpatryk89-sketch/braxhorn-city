import * as T from 'three';
import {World, STEP_UP} from '../src/world.js';

// The original level's door openings, stairs and furnished interiors now collide
// with a Fighter capsule, rather than with an eye-height spectator camera.
export class EstateWorld extends World {
  constructor(level) {
    super(); this.level=level; this.ray=new T.Raycaster(); this.down=new T.Vector3(0,-1,0);
    this.previous=new T.Vector3(); this.groundCache=null;
    for(const b of level.world.collision)this.box(b.minX,b.maxX,b.minZ,b.maxZ,b.maxY,b.minY);
  }
  reset(position) {this.previous.copy(position);this.groundCache=null;}
  nearby(x,z,padding=1) {
    return this.level.imported.filter(o=>{const b=o.userData.collisionBounds;return x>=b.min.x-padding&&x<=b.max.x+padding&&z>=b.min.z-padding&&z<=b.max.z+padding;});
  }
  meshes(models) {return models.flatMap(o=>o.userData.collisionMeshes);}
  importedBlocked(from,to,r,y,h) {
    const direction=new T.Vector3(to.x-from.x,0,to.z-from.z),length=direction.length();
    if(length<1e-7)return false;
    const meshes=this.meshes(this.nearby(from.x,from.z,length+r));
    if(!meshes.length)return false;
    direction.divideScalar(length);const side=new T.Vector3(-direction.z,0,direction.x);
    for(const offset of [-r*.85,0,r*.85])for(const height of [STEP_UP+.06,h*.5,h-.12]) {
      this.ray.set(new T.Vector3(from.x+side.x*offset,y+height,from.z+side.z*offset),direction);
      this.ray.near=0;this.ray.far=length+r*.75;
      if(this.ray.intersectObjects(meshes,false).length)return true;
    }
    return false;
  }
  resolve(pos,r,y,h,ignore,ignore2) {
    let hit=super.resolve(pos,r,y,h,ignore,ignore2);
    if(this.level.doors?.resolve(pos,r,y,h))hit=true;
    const from=this.previous.clone(),desired=pos.clone();
    for(const axis of ['x','z']) {
      const to=from.clone();to[axis]=desired[axis];
      if(!this.importedBlocked(from,to,r,y,h))from.copy(to);else hit=true;
    }
    pos.x=from.x;pos.z=from.z;this.previous.copy(pos);return hit;
  }
  groundAt(x,z,y,r=0) {
    const cache=this.groundCache;
    if(cache&&Math.abs(x-cache.x)<1e-6&&Math.abs(z-cache.z)<1e-6&&Math.abs(y-cache.y)<1e-6)return cache.value;
    let value=super.groundAt(x,z,y,r);
    this.ray.set(new T.Vector3(x,y+STEP_UP+.02,z),this.down);this.ray.near=0;this.ray.far=3;
    const floor=this.ray.intersectObjects(this.meshes(this.nearby(x,z,.1)),false).find(hit=>hit.face&&hit.face.normal.clone().transformDirection(hit.object.matrixWorld).y>.55);
    if(floor&&floor.point.y>=-.1)value=Math.max(value,floor.point.y);
    for(const f of this.level.world.walkable) {
      const inside=f.radius?Math.hypot(x-f.x,z-f.z)<=f.radius:x>=f.x0&&x<=f.x1&&z>=f.z0&&z<=f.z1;
      if(inside&&f.y<=y+STEP_UP)value=Math.max(value,f.y);
    }
    this.groundCache={x,z,y,value};return value;
  }
  cameraPosition(target,desired) {
    const direction=desired.clone().sub(target),length=direction.length();direction.normalize();
    const ray=new T.Ray(target,direction),point=new T.Vector3();let distance=length;
    for(const b of this.boxes) {
      if(b.top-b.bottom<1.8)continue;
      const box=new T.Box3(new T.Vector3(b.minX-.15,b.bottom-.1,b.minZ-.15),new T.Vector3(b.maxX+.15,b.top+.1,b.maxZ+.15));
      if(box.containsPoint(target))continue;
      if(ray.intersectBox(box,point))distance=Math.min(distance,Math.max(.65,point.distanceTo(target)-.25));
    }
    this.ray.set(target,direction);this.ray.near=.3;this.ray.far=length;
    const hits=[this.ray.intersectObjects(this.meshes(this.nearby(target.x,target.z,length)),false)[0],this.level.doors?.cameraHit(this.ray)].filter(Boolean);
    const hit=hits.sort((a,b)=>a.distance-b.distance)[0];
    if(hit)distance=Math.min(distance,Math.max(.65,hit.distance-.25));
    return target.clone().addScaledVector(direction,distance);
  }
}
