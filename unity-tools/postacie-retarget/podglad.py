"""Render complete Blender actions as a labelled film with a stationary camera."""
import argparse
import math
import sys
import tempfile
import subprocess
from pathlib import Path
import bpy
import numpy as np
from mathutils import Vector
sys.path.insert(0, str(Path(__file__).resolve().parent))
from brax_animacje import rest_data, meshes_of, scene_camera, mesh_vertices, unit


def main():
    p = argparse.ArgumentParser()
    p.add_argument('--ruch', nargs='+', required=True)
    p.add_argument('--film', type=Path, required=True)
    p.add_argument('--kat', type=float, default=35)
    p.add_argument('--fps', type=int, default=24)
    opt = p.parse_args(sys.argv[sys.argv.index('--') + 1:])
    final = opt.film.resolve()
    if final.exists():
        raise FileExistsError(final)
    if opt.fps <= 0:
        raise ValueError('fps must be positive')
    final.parent.mkdir(exist_ok=True, parents=True)
    rig = next(o for o in bpy.context.scene.objects if o.type == 'ARMATURE')
    scene = bpy.context.scene
    rate = scene.render.fps / scene.render.fps_base
    meshes = meshes_of(rig)
    cam = scene_camera(meshes)
    axes = rest_data(rig)['body']
    up = axes[:, 1]
    theta = math.radians(opt.kat)
    view = unit(axes[:, 2] * math.cos(theta) + axes[:, 0] * math.sin(theta))
    right = unit(np.cross(view, up))
    basis = np.column_stack((right, up, view))
    scene.render.resolution_x = 480
    scene.render.resolution_y = 560
    scene.render.image_settings.quality = 90
    scene.display.render_aa = 'FXAA'

    def setframe(f):
        scene.frame_set(math.floor(f), subframe=f - math.floor(f))
    with tempfile.TemporaryDirectory(dir=final.parent) as tmp:
        folder = Path(tmp)
        segments = []
        for idx, label in enumerate(opt.ruch):
            action = bpy.data.actions.get(label)
            if not action:
                raise ValueError('Missing action ' + label)
            rig.animation_data_create().action = action
            a, b = action.frame_range
            low = []
            high = []
            for f in np.linspace(a, b, 25):
                setframe(float(f))
                deps = bpy.context.evaluated_depsgraph_get()
                for ob in meshes:
                    ev = ob.evaluated_get(deps)
                    mat = np.array(ev.matrix_world)
                    allpts = (mesh_vertices(ev) @ mat[:3, :3].T + mat[:3, 3]) @ basis
                    low.append(allpts.min(0))
                    high.append(allpts.max(0))
            lo = np.min(low, axis=0)
            hi = np.max(high, axis=0)
            center = basis @ ((lo + hi) / 2)
            scale = max((hi - lo)[1], (hi - lo)[0] * 560 / 480) * 1.13
            cam.location = Vector(center + view * scale * 3)
            cam.rotation_euler = Vector(-view).to_track_quat('-Z', 'Y').to_euler()
            cam.data.ortho_scale = scale
            cam.data.clip_end = 8 * scale
            segment = folder / str(idx)
            segment.mkdir()
            frames = max(2, round((b - a) * opt.fps / rate) + 1)
            for t, f in enumerate(np.linspace(a, b, frames)):
                setframe(float(f))
                scene.render.filepath = str(segment / f'{t:05d}.jpg')
                bpy.ops.render.render(write_still=True)
            output = folder / f'{idx}.mp4'
            # The caption is drawn from an external text file, independent of action-name punctuation.
            caption = segment / 'label.txt'
            caption.write_text(action.get('source_name', label), encoding='utf-8')
            cmd = ['ffmpeg', '-v', 'error', '-y', '-framerate', str(opt.fps), '-i', '%05d.jpg', '-vf',
                   "pad=480:616:0:0:color=0x15242e,drawtext=textfile=label.txt:expansion=none:fontcolor=white:fontsize=20:x=15:y=577",
                   '-c:v', 'libx264', '-pix_fmt', 'yuv420p', '-threads', '2', str(output)]
            subprocess.run(cmd, cwd=segment, check=True)
            segments.append(output)
        listing = folder / 'concat.txt'
        listing.write_text(''.join(f"file '{x.name}'\n" for x in segments))
        temp = folder / 'final.mp4'
        subprocess.run(['ffmpeg', '-v', 'error', '-y', '-f', 'concat', '-safe', '1', '-i',
                       str(listing), '-c', 'copy', '-movflags', '+faststart', str(temp)], check=True)
        temp.rename(final)


if __name__ == '__main__':
    main()
