"""Preview camera views for one apartment building (building frame, nominal metres, front = -Z).
usage: python3 views.py T04 > views_T04.json"""
import json
import sys

SIZE = {"T04": (16.0, 8.5), "T05": (17.0, 9.0), "T06": (17.0, 9.0), "T07": (17.0, 9.0)}
GROUND, STOREY, FFL0 = 4.20, 3.15, .30


def level(f):
    return FFL0 if f <= 0 else FFL0 + GROUND + (f - 1) * STOREY


def views(bid):
    hw, hd = SIZE[bid]
    l1 = level(1); zin = -hd + 1.15
    v = [
        ("front34", [-34, 11, -46], [0, 7, 0], 48),
        ("rear34", [36, 15, 40], [0, 7, 0], 48),
        ("corner", [-hw - 5.5, 2.2, -hd - 7.5], [-hw + 1, 3.0, -hd + 1], 58),
        ("entrance", [1.5, 1.7, -hd - 7.0], [0, 2.2, -hd + 2], 62),
        ("lobby", [.8, 1.75, zin + .5], [-.4, 1.55, -1.0], 82),
        ("mailboxes", [1.8, 1.65, -6.2], [-3.0, 1.45, -3.4], 72),
        ("corridor", [-hw + .9, l1 + 1.65, .2], [hw, l1 + 1.45, .2], 72),
        ("stair", [1.2, l1 + 1.7, 3.0], [-2.4, l1 + .6, 6.0], 84),
        ("living", [-hw + 4.5, l1 + 1.6, -1.4], [-hw + 2.5, l1 + 1.25, -hd], 84),
        ("bedroom", [-7.2, l1 + 1.6, -3.4], [-8.0, l1 + 1.2, -hd], 84),
    ]
    # bathroom of the front-left apartment: x from uHall to the inner end (inner end -3.2 with a lobby/core lounge, else -.1)
    x1 = -3.2 if bid == "T06" else -.1
    v.append(("bath", [x1 - .35, l1 + 1.6, -1.0], [x1 - 1.9, l1 + .85, -3.0], 92))
    return [dict(out=f"{bid}_{n}.png", eye=e, target=t, fov=f) for n, e, t, f in v]


if __name__ == "__main__":
    print(json.dumps(views(sys.argv[1]), indent=1))
