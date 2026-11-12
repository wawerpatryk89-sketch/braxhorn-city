#!/usr/bin/env python3
"""A Mixamo FBX downloaded 'with the character' (owner 12.11.2026: "robię Without Skin i tak mi ściąga z buldogiem") carries
the character's embedded textures: ~35 of its ~40 MB. The game takes only the motion from it (Unity Humanoid import), so
before the file is packed the embedded image bytes are dropped (Video.Content -> empty). The skeleton, the animation
curves, the mesh and every object and connection stay exactly as they were; the file stays a valid binary FBX 7.x
(every node's end offset is recomputed). Files without embedded images are copied unchanged.
usage: fbx_bez_tekstur.py <in.fbx> <out.fbx>        prints the sizes; exit 1 if the input is not a binary FBX
"""
import struct
import sys

MAGIC = b"Kaydara FBX Binary  \x00\x1a\x00"


def read_props(b, p, n):
    props = []
    for _ in range(n):
        t = b[p:p + 1]
        start = p
        p += 1
        if t in (b"Y",):
            p += 2
        elif t in (b"C",):
            p += 1
        elif t in (b"I", b"F"):
            p += 4
        elif t in (b"D", b"L"):
            p += 8
        elif t in (b"f", b"d", b"l", b"i", b"b"):
            _, _, clen = struct.unpack_from("<III", b, p)
            p += 12 + clen
        elif t in (b"S", b"R"):
            (ln,) = struct.unpack_from("<I", b, p)
            p += 4 + ln
        else:
            raise ValueError("unknown property type %r at %d" % (t, start))
        props.append(b[start:p])
    return props, p


def parse(b, p, wide):
    """returns (node or None at the null record, next position)"""
    hdr = 25 if wide else 13
    if wide:
        end, nprops, plen = struct.unpack_from("<QQQ", b, p)
    else:
        end, nprops, plen = struct.unpack_from("<III", b, p)
    nlen = b[p + hdr - 1]
    if end == 0:
        return None, p + hdr
    name = b[p + hdr:p + hdr + nlen]
    q = p + hdr + nlen
    props, q2 = read_props(b, q, nprops)
    if q2 != q + plen:
        raise ValueError("property length mismatch in " + name.decode("latin1"))
    kids = []
    q = q2
    while q < end:
        k, q = parse(b, q, wide)
        if k is None:
            break
        kids.append(k)
    return {"name": name, "props": props, "kids": kids, "hadNull": q2 < end}, end


def size(node, wide):
    hdr = 25 if wide else 13
    s = hdr + len(node["name"]) + sum(len(x) for x in node["props"])
    if node["kids"] or node["hadNull"]:
        s += sum(size(k, wide) for k in node["kids"]) + hdr
    return s


def write(node, out, pos, wide):
    hdr = 25 if wide else 13
    end = pos + size(node, wide)
    plen = sum(len(x) for x in node["props"])
    if wide:
        out += struct.pack("<QQQ", end, len(node["props"]), plen)
    else:
        out += struct.pack("<III", end, len(node["props"]), plen)
    out += bytes([len(node["name"])]) + node["name"]
    for x in node["props"]:
        out += x
    p = pos + hdr + len(node["name"]) + plen
    if node["kids"] or node["hadNull"]:
        for k in node["kids"]:
            p = write(k, out, p, wide)
        out += b"\x00" * hdr
        p += hdr
    return end


def strip(node, dropped):
    if node["name"] == b"Video":
        for k in node["kids"]:
            if k["name"] == b"Content" and k["props"] and k["props"][0][:1] == b"R":
                dropped[0] += len(k["props"][0])
                k["props"] = [b"R" + struct.pack("<I", 0)]
    for k in node["kids"]:
        strip(k, dropped)


def main():
    src, dst = sys.argv[1], sys.argv[2]
    b = open(src, "rb").read()
    if not b.startswith(MAGIC):
        print("NOT_BINARY_FBX " + src)
        return 1
    (version,) = struct.unpack_from("<I", b, 23)
    wide = version >= 7500
    hdr = 25 if wide else 13
    p = 27
    top = []
    while True:
        n, p = parse(b, p, wide)
        if n is None:
            break
        top.append(n)
    footer = b[p:]
    dropped = [0]
    for n in top:
        strip(n, dropped)
    if dropped[0] == 0:
        open(dst, "wb").write(b)
        print("UNCHANGED %s %.1f MB" % (src, len(b) / 1048576))
        return 0
    out = bytearray(b[:27])
    pos = 27
    for n in top:
        pos = write(n, out, pos, wide)
    out += b"\x00" * hdr
    # footer: its 16-byte id, 4 zero bytes, zero padding that puts the id's start on a 16-byte boundary (as the FBX SDK
    # writes it: start % 16 = 8 -> 8 bytes, 7 -> 9), then the version, 120 zero bytes and the closing magic, unchanged
    vi = footer.find(struct.pack("<I", version), 20)
    if len(footer) < 20 or vi < 0:
        raise ValueError("unexpected FBX footer")
    pad = (16 - len(out) % 16) % 16
    out += footer[:16] + b"\x00" * (4 + pad) + footer[vi:]
    open(dst, "wb").write(out)
    print("STRIPPED %s %.1f MB -> %.1f MB" % (src, len(b) / 1048576, len(out) / 1048576))
    return 0


if __name__ == "__main__":
    sys.exit(main())
