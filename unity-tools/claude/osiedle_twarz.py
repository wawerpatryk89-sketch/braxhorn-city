"""Build per-character facial poses from that character's original UVs and skin texture."""

import json
import math
import struct
import zlib
from pathlib import Path

import bpy
import numpy as np

# Front-view pins on the neutral source mesh, in a 256-pixel orthographic head frame.
# Eyes are (x, y, half width, half height, inner-to-outer vertical slope).
# Mouth is (x, central lip seam, half width, corner drop, opening, visible teeth).
FACES = {
    "aneta": ([(88, 126, 15, 4, 1), (150, 126, 15, 4, -1)], (118, 186, 26, -3, 8, 0)),
    "bramkarz_1c96d333": (
        [(91, 111, 12, 3, -1), (154, 110, 12, 3, 1)],
        (124, 168, 24, 2, 8, 0),
    ),
    "dziecko_003f4e46": (
        [(84, 165, 14, 5, -1), (156, 165, 13, 5, 0)],
        (120, 234, 32, -5, 7, 1),
    ),
    "ewa": ([(94, 138, 14, 5, -1), (158, 135, 15, 6, -1)], (129, 198, 26, 1, 8, 0)),
    "gitara": ([(95, 124, 12, 3, -1), (150, 124, 12, 3, 0)], (122, 183, 27, 1, 8, 0)),
    "imprezowiczka_0dbe3922": (
        [(92, 115, 15, 4, 1), (152, 115, 15, 4, -1)],
        (122, 173, 27, 0, 8, 0),
    ),
    "imprezowiczka_11baefe5": (
        [(92, 101, 14, 4, 1), (153, 101, 14, 4, -1)],
        (122, 153, 24, -3, 7, 1),
    ),
    "imprezowiczka_13e8d41d": (
        [(92, 112, 14, 4, 1), (152, 111, 15, 4, -1)],
        (122, 167, 20, 1, 8, 0),
    ),
    "kamil": ([(94, 113, 12, 3, 0), (154, 114, 12, 3, 1)], (125, 173, 25, 0, 8, 0)),
    "kapitan_02d85d24": (
        [(94, 134, 12, 3, -1), (148, 133, 12, 3, 1)],
        (121, 197, 25, 1, 7, 0),
    ),
    "kiler": ([(92, 118, 11, 3, 0), (151, 118, 11, 3, 0)], (123, 176, 25, -2, 7, 0)),
    "martyna": ([(88, 129, 14, 4, 0), (154, 129, 14, 4, -1)], (121, 193, 24, 0, 8, 0)),
    "mateusz": ([(95, 124, 13, 3, 0), (152, 123, 12, 3, 0)], (124, 185, 24, -2, 8, 0)),
    "monika": ([(93, 135, 14, 4, 0), (157, 133, 15, 4, -1)], (125, 197, 27, -4, 8, 0)),
    "patryk": ([(94, 131, 13, 4, -1), (153, 132, 12, 3, 1)], (123, 196, 28, -5, 8, 0)),
    "plazowicz_0493f781": (
        [(92, 118, 13, 3, 0), (152, 117, 12, 3, 0)],
        (123, 183, 24, 0, 8, 0),
    ),
    # Dark sunglasses are part of the owner's mesh and remain opaque.
    "wiechu": ([], (120, 188, 28, 0, 7, 0)),
    "zadymiarz_021fef69": (
        [(77, 92, 12, 8, -3), (165, 92, 12, 8, 3)],
        (121, 149, 41, 37, 7, 0),
    ),
    "zadymiarz_163dca80": (
        [(92, 135, 16, 5, 1), (153, 136, 16, 5, -2)],
        (124, 196, 23, 0, 8, 0),
    ),
}
# Catalog IDs distinguish two exports that share the same internal actor name.
CATALOG = json.loads(Path(__file__).with_name("profile_twarzy.json").read_text(encoding="utf-8"))


def face_profile(actor, override=None):
    if override is not None:
        return override
    if actor in FACES:
        return FACES[actor]
    key = actor.rsplit("_", 1)[-1]
    for name, pins in FACES.items():
        if name.rsplit("_", 1)[-1] == key:
            return pins
    if key in CATALOG:
        return CATALOG[key]
    raise ValueError(f"{actor}: supply calibrated eye and mouth landmarks from its neutral portrait")


SIZE = 256
LEVELS = 8


def sample(image, x, y):
    x = np.clip(x, 0, image.shape[1] - 1.001)
    y = np.clip(y, 0, image.shape[0] - 1.001)
    ix, iy = x.astype(int), y.astype(int)
    a, b = (x - ix)[..., None], (y - iy)[..., None]
    return (1 - b) * ((1 - a) * image[iy, ix] + a * image[iy, ix + 1]) + b * (
        (1 - a) * image[iy + 1, ix] + a * image[iy + 1, ix + 1]
    )


def front_image(points, tris, triangle_uv, image):
    depth = np.full((SIZE, SIZE), -np.inf)
    front = np.zeros((SIZE, SIZE, 3), np.float32)
    for triangle, uv in zip(points[tris], triangle_uv):
        a, b, c = triangle
        lo = np.maximum(0, np.floor(triangle[:, :2].min(axis=0))).astype(int)
        hi = np.minimum(SIZE - 1, np.ceil(triangle[:, :2].max(axis=0))).astype(int)
        if np.any(lo > hi):
            continue
        ab, ac = b - a, c - a
        det = ab[0] * ac[1] - ab[1] * ac[0]
        if abs(det) < 1e-6:
            continue
        xx, yy = np.meshgrid(np.arange(lo[0], hi[0] + 1), np.arange(lo[1], hi[1] + 1))
        dx, dy = xx + 0.5 - a[0], yy + 0.5 - a[1]
        v = (dx * ac[1] - dy * ac[0]) / det
        w = (ab[0] * dy - ab[1] * dx) / det
        u = 1 - w - v
        z = u * a[2] + v * b[2] + w * c[2]
        take = (u >= 0) & (v >= 0) & (w >= 0) & (z > depth[yy, xx])
        if not take.any():
            continue
        y, x = yy[take], xx[take]
        depth[y, x] = z[take]
        tex = u[take, None] * uv[0] + v[take, None] * uv[1] + w[take, None] * uv[2]
        front[y, x] = sample(
            image,
            (tex[:, 0] % 1) * image.shape[1] - 0.5,
            (tex[:, 1] % 1) * image.shape[0] - 0.5,
        )
    return front, depth


def speech_teeth(cavity, dx, depth, opening, t, lip_color, muzzle=False):
    # Upper incisors emerge from behind the upper lip. The mouth corners
    # stay in shadow and the lower row is covered in conversational speech.
    side = np.abs(dx)
    amount = np.clip((t - .28) / .52, 0, 1)
    shade = np.clip(np.mean(lip_color, axis=2) * .28 + .46, .48, .63)
    enamel = shade[..., None] * np.array([1., .94, .83])
    result = cavity.copy()
    teeth = ((.115,.117,1.),(.325,.092,.81),(.487,.075,.62),(.62,.067,.42))
    if muzzle:
        teeth = ((.105,.092,.51),(.295,.082,.48),(.52,.078,1.14))
    for center, width, length in teeth:
        d = np.abs(side-center)/width
        crown = np.clip(1-d**4,0,1)**.30
        arch = (.76+.24*crown) * length * np.minimum(opening*.44,2.75)
        if muzzle and length > 1:
            arch *= np.clip(1 - .83*d, 0, 1)
        opacity = np.clip((1.13-d)*4,0,1)*np.clip(arch-depth+.42,0,1)*amount
        tint = enamel * (.87+.09*crown-.15*side)[...,None]
        result += (tint-result)*opacity[...,None]
    return result


def expression(front, eyes, mouth, t, custom_teeth=False):
    yy, xx = np.indices(front.shape[:2],dtype=float)
    rgb = front.copy()
    alpha = np.zeros_like(xx)
    for ex,ey,width,height,slope in eyes:
        dx=(xx-ex)/(width+2.3)
        arc=np.sqrt(np.maximum(0,1-dx*dx))
        center=ey+slope*dx
        top=center-(height+2.25)*arc
        bottom=center+(height+2.15)*arc
        seam=center+height*.29*arc
        upper=top+(seam-top)*t
        lower=bottom+(seam-bottom)*t
        feather=np.clip((1-np.abs(dx))*(width+2.3),0,1)
        # Draw the advancing lid from skin above the lash line. Sampling the
        # open-eye texture on the moving edge leaves the iris on the closed lid.
        local = np.clip(yy-top+1.,0,1)*np.clip(bottom-yy+1.,0,1)*feather
        # The narrow strip between eyelashes and brows varies considerably
        # between scans. Use the adjoining lower lid to fill the lid surface;
        # exclude the eyebrows and the edges of eyeglass frames from the mask.
        base = sample(front,xx,bottom+2.7*arc+.65)
        near = sample(front,xx,top-1.35*arc-.65)
        skin = base*.81 + near*.19
        if mouth[-1] and ey>150:
            blue_frame=(front[:,:,2]>front[:,:,0]*1.26)&(front[:,:,2]>front[:,:,1]*1.18)
            local *= ~blue_frame
        upper_mask=np.clip(upper-yy+.6,0,1)
        lower_mask=np.clip(yy-lower+.6,0,1)
        cover=np.maximum(upper_mask,lower_mask)*local*min(1.,t*6)
        if t>.92:
            cover=np.maximum(cover,local*(t-.92)/.08)
        depth=np.clip((yy-top)/np.maximum(1.,seam-top),0,1)
        surface=skin*(1.01-.07*depth)[...,None]
        line=np.exp(-((yy-upper)/(.46+.34*arc))**2)*arc*feather*t
        surface*=1.-.43*line[...,None]
        rgb+=(surface-rgb)*cover[...,None]
        alpha=np.maximum(alpha,cover)
    mx,my,width,corner,excursion,teeth=mouth
    dx=(xx-mx)/width
    shape=np.maximum(0,1-dx*dx)
    line=my+corner*np.minimum(dx*dx,1)
    if teeth:
        # The scan already supplies its own teeth. Put the hinge below the
        # last enamel pixel so opening the lip cannot duplicate that row.
        offsets=np.arange(-4.,10.51,.5)
        rows=np.stack([sample(front,xx[:1],line[:1]+dy)[0] for dy in offsets])
        light=rows@np.array([.22,.65,.13])
        ivory=(light>.49)&((rows[:,:,0]-rows[:,:,2])<.23)
        bottom=np.max(np.where(ivory,offsets[:,None],-8.),axis=0)
        bottom=np.where(bottom>-7,bottom+.65,2.4)
        line=line+np.convolve(bottom,[.13,.22,.30,.22,.13],mode='same')[None,:]*shape

    # Preserve each scan's lip seam and the position of the corners.
    # Follow the local dark seam within a small band, excluding a beard or moustache.
    if not teeth and abs(corner)<12:
        offsets=np.arange(-2.5,2.51,.5)
        samples=np.stack([sample(front,xx[:1],line[:1]+off)[0] for off in offsets])
        lum=samples@np.array([.23,.60,.17])
        k=np.argmin(lum+.009*np.abs(offsets)[:,None],axis=0)
        seam=np.convolve(offsets[k],[.1,.2,.4,.2,.1],mode='same')
        line=line+seam[None,:]*shape
    opening=t*excursion*.78*shape**1.52
    if teeth:
        # These two neutral scans already have a smiling, partly open mouth.
        # A little extra lower-lip travel suffices without exaggerating the smile.
        opening *= .38
    below=yy-line
    margin=np.clip((1-np.abs(dx))/.19,0,1)
    blend=margin*np.clip((10+opening-below)/6,0,1)*np.clip(below+.65,0,1)
    lower=sample(front,xx,np.maximum(yy-opening,line+.50) if teeth else yy-opening)
    inner=np.clip(below+.4,0,1)*np.clip(opening-below+.38,0,1)
    lip=sample(front,xx,line+3)
    cavity=lip*np.array([.38,.23,.26])
    if custom_teeth:
        cavity=speech_teeth(cavity,dx,below,opening,t,lip,muzzle=abs(corner)>=12)
    lower+=(cavity-lower)*inner[...,None]
    rgb+=(lower-rgb)*blend[...,None]
    alpha=np.maximum(alpha,blend)
    # The Unity shader applies the atlas alpha once against the base skin.
    # Convert the composited preview back to straight alpha before saving.
    rgb = front + (rgb-front)/np.maximum(alpha[...,None], 1e-6)
    if t==0:
        alpha.fill(0)
    return np.uint8(np.clip(np.dstack((rgb,alpha))*255+.5,0,255))


def png(path, rgba, clear_transparent=True):
    def chunk(tag, data):
        return (
            struct.pack(">I", len(data))
            + tag
            + data
            + struct.pack(">I", zlib.crc32(tag + data) & 0xFFFFFFFF)
        )

    # Use a transparent border / gutter around each active region.
    if clear_transparent:
        rgba[rgba[:, :, 3] == 0] = 0
    height, width = rgba.shape[:2]
    raw = b"".join(b"\0" + row.tobytes() for row in rgba)
    Path(path).write_bytes(
        b"\x89PNG\r\n\x1a\n"
        + chunk(b"IHDR", struct.pack(">IIBBBBB", width, height, 8, 6, 0, 0, 0))
        + chunk(b"IDAT", zlib.compress(raw, 6))
        + chunk(b"IEND", b"")
    )


def head_projection(obj, axes, head, color_path):
    mesh = obj.data
    mesh.calc_loop_triangles()
    co = np.array([v.co[:] for v in mesh.vertices])
    transform = np.asarray(obj.matrix_world)
    body = (co @ transform[:3, :3].T + transform[:3, 3]) @ axes
    span = np.ptp(body[:, 1]) * 0.175
    head = head @ axes
    cx, cy = head[0], body[:, 1].max() - span * 0.48
    points = np.column_stack(
        (
            (body[:, 0] - cx) / span + 0.5,
            0.5 - (body[:, 1] - cy) / span,
            body[:, 2] / span,
        )
    )
    points[:, :2] *= SIZE
    triangles = np.array([t.vertices[:] for t in mesh.loop_triangles])
    texture_uv = np.array([p.uv[:] for p in mesh.uv_layers.active.data])
    loops = np.array([t.loops[:] for t in mesh.loop_triangles])
    image = bpy.data.images.load(str(color_path), check_existing=False)
    image.colorspace_settings.name = "Non-Color"
    pixels = np.empty(len(image.pixels), np.float32)
    image.pixels.foreach_get(pixels)
    color = pixels.reshape(image.size[1], image.size[0], 4)[:, :, :3]
    front, depth = front_image(points, triangles, texture_uv[loops], color)
    bpy.data.images.remove(image)
    return co, transform, body, span, head, points, triangles, loops, front, depth


def add_face(actor, obj, axes, head, color_path, atlas_path, profile=None):
    mesh = obj.data
    eyes, mouth = face_profile(actor, profile)
    co, transform, body, span, head, points, triangles, loops, front, depth = head_projection(obj, axes, head, color_path)
    eye_y = np.mean([e[1] for e in eyes]) if eyes else mouth[1] - 58
    shift = round(128 - (eye_y + mouth[1]) / 2)
    # Only the frontal surface is mapped. Keep the back of the head, hair, and glasses outside the atlas.
    ix, iy = np.clip(points[:, :2].astype(int), 0, SIZE - 1).T
    visible = (points[:, 2] > depth[iy, ix] - 0.13) & (
        points[:, 2] > (head[2] / span - 0.02)
    )
    coordinates = np.column_stack(
        (points[:, 0] / SIZE, 1 - (points[:, 1] + shift) / SIZE)
    )
    # Apply visibility per triangle so hidden vertices cannot stretch the atlas across an ear or the scalp.
    if len(mesh.uv_layers) > 1 and mesh.uv_layers[1].name != "Mimika":
        raise ValueError(f"{actor}: UV2 already belongs to {mesh.uv_layers[1].name}")
    layer = mesh.uv_layers.get("Mimika") or mesh.uv_layers.new(name="Mimika", do_init=False)
    projected = np.full((len(mesh.loops), 2), -2, dtype=np.float32)
    for tri, loop in zip(triangles, loops):
        if visible[tri].all():
            projected[loop] = coordinates[tri]
    layer.data.foreach_set("uv", projected.ravel())
    mesh.uv_layers.active_index = 0
    mesh.uv_layers[0].active_render = True
    levels = [
        expression(front, eyes, mouth, t / (LEVELS - 1), not mouth[-1])
        for t in range(LEVELS)
    ]
    atlas = np.zeros((SIZE * 2, SIZE * 4, 4), np.uint8)
    # Integer shift keeps camera-space pixel centres aligned with the second UV set.
    for i, rgba in enumerate(levels):
        yy = np.arange(SIZE) - shift
        shifted = sample(rgba.astype(float), *np.meshgrid(np.arange(SIZE), yy))
        atlas[
            (i // 4) * SIZE : (i // 4 + 1) * SIZE, (i % 4) * SIZE : (i % 4 + 1) * SIZE
        ] = np.uint8(shifted)
    png(atlas_path, atlas)
    if not mesh.shape_keys:
        obj.shape_key_add(name="Basis")
    key = mesh.shape_keys.key_blocks.get("BraxJaw") or obj.shape_key_add(name="BraxJaw", from_mix=False)
    mx, my, width, corner, opening, _ = mouth
    dx = abs(points[:, 0] - mx) / width
    line = my + corner * np.minimum(dx**2, 1)
    along = np.clip((points[:, 1] - line) / 10, 0, 1) * np.clip(
        (line + 58 - points[:, 1]) / 27, 0, 1
    )
    across = np.clip((1.8 - dx) / 0.9, 0, 1)
    face_depth = np.clip((body[:, 2] - head[2]) / (span * 0.14), 0, 1)
    weight = along * across * face_depth
    # A small jaw rotation shifts the chin down and back. Lip separation is drawn from its own atlas.
    movement = (-axes[:, 1] * 0.82 - axes[:, 2] * 0.27) * span * opening / SIZE
    local = np.linalg.solve(transform[:3, :3], movement)
    key.data.foreach_set("co", (co + weight[:, None] * local).astype("f").ravel())
    key.value = 0
    # Approximate the rounded Polish o/u vowels with local inward and forward lip motion.
    rounded = mesh.shape_keys.key_blocks.get("BraxRound") or obj.shape_key_add(name="BraxRound", from_mix=False)
    lip_band = np.clip(1 - abs(points[:, 1] - line) / (opening + 6), 0, 1)
    lip_band *= np.clip(1 - (dx / 1.35) ** 2, 0, 1) * face_depth
    inward = -(points[:, 0] - mx) * span / SIZE * 0.22
    motion = (inward[:, None] * axes[:, 0] + span * 0.009 * axes[:, 2]) * lip_band[
        :, None
    ]
    local_round = motion @ np.linalg.inv(transform[:3, :3]).T
    rounded.data.foreach_set("co", (co + local_round).astype("f").ravel())
    rounded.value = 0
    if weight.max() < 0.5 or not np.any(atlas[:, :, 3]):
        raise ValueError(actor + " missing jaw or face coverage")
