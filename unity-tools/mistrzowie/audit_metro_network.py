"""Geometric proof for full 5-station metro rail route and source portals.

Physics, prefab UVs, train movement and Android performance still require Unity.
No claim about deeper foundations unless their geometry exists in source.
"""
from __future__ import annotations

import json
import math
from pathlib import Path
from shapely.geometry import LineString, Point, box

from layout import (
    METRO, METRO_LINE_WAYPOINTS, METRO_ROUTE_ORDER,
    METRO_TRACK_OFFSET, GAUGE, K, station_point,
)
from meshkit import rounded_route

ROOT=Path(__file__).resolve().parents[2]
RAIL_BORE_HALF_WIDTH=(10.4+7.6)/K+0.7   # outer walls + margin, nominal m
PORTAL_START=-70.0
PORTAL_END=112.0
PORTAL_Z=28.5
EXPECTED_ORDER=("M5","M1","M4","M3","M2")


def station_portal_points(station):
    return (Point(station_point(station,PORTAL_START,PORTAL_Z)),
            Point(station_point(station,PORTAL_END,PORTAL_Z)))


def station_box(station):
    corners=[station_point(station,x,z)
             for x in (PORTAL_START,PORTAL_END) for z in (17,40)]
    return box(min(x for x,z in corners),min(z for x,z in corners),
               max(x for x,z in corners),max(z for x,z in corners))


def assess():
    line=LineString(rounded_route(METRO_LINE_WAYPOINTS,60,3.0,False))
    if not line.is_simple or line.length<1600:
        raise ValueError("Metro centre line must be connected, simple and city-spanning")
    if tuple(METRO_ROUTE_ORDER)!=EXPECTED_ORDER:
        raise ValueError("Station timetable order differs from authored geometry")
    if len(METRO)!=5 or {m["id"] for m in METRO}!={"M1","M2","M3","M4","M5"}:
        raise ValueError("Five original-size metro stations required")
    records=[]
    occupied=[]
    for station in METRO:
        centre=Point(station["x"],station["z"])
        if line.distance(centre)>0.03:
            raise ValueError(station["id"]+" centre not on authored metro line")
        portal_a,portal_b=station_portal_points(station)
        capsule=station_box(station)
        stretch=line.intersection(capsule)
        if stretch.geom_type!="LineString" or stretch.is_empty:
            raise ValueError(station["id"]+" metro rails do not cross prefab platform")
        start=Point(stretch.coords[0]);end=Point(stretch.coords[-1])
        if min(start.distance(portal_a)+end.distance(portal_b),
               start.distance(portal_b)+end.distance(portal_a))>.16:
            raise ValueError(station["id"]+" rail tunnel portals are not flush with prefab source")
        native_length=(PORTAL_END-PORTAL_START)/K
        if abs(stretch.length-native_length)>.2:
            raise ValueError(station["id"]+" has a curve inside the station track")
        distance=line.project(centre)
        # Guard against a curve placed directly against a full-size platform.
        for station_edge in (start,end):
            d=line.project(station_edge)
            before=line.interpolate(max(0,d-8))
            after=line.interpolate(min(line.length,d+8))
            heading=(after.x-before.x, after.y-before.y)
            size=math.hypot(*heading)
            if size<.001:
                raise ValueError("Degenerate station transition "+station["id"])
            component=abs(heading[0]/size) if station["axis"]=="x" else abs(heading[1]/size)
            if component<.999:
                raise ValueError(station["id"]+" curve too near physical rail portal")
        occupied.append(stretch)
        records.append(dict(id=station["id"],name=station["name"],
                            centre=[station["x"],station["z"]],
                            routeMetres=round(distance,1),
                            originalPortalMetres=round(stretch.length,1)))
    records.sort(key=lambda s:s["routeMetres"])
    if tuple(x["id"] for x in records)!=EXPECTED_ORDER:
        raise ValueError("Wrong station order along original two-rail alignment")
    if min(b["routeMetres"]-a["routeMetres"] for a,b in zip(records,records[1:]))<150:
        raise ValueError("Metro stations too close together")

    # Only a measured conflict with existing BELOW-GROUND casino structures
    # is an absolute blocker. Overground plots may have underground tunnels.
    casino=box(495-60,465-50,495+60,465+50)
    parking_b1=box(495-36.5,465-26.5,495+36.5,465+26.5)
    garage_ramp=box(535.5,443,543.5,489)
    safety=line.buffer(RAIL_BORE_HALF_WIDTH,cap_style="flat",join_style=1)
    if safety.intersects(casino) or safety.intersects(parking_b1) or safety.intersects(garage_ramp):
        raise ValueError("Metro tube conflicts with T11 casino underground parking reservation")

    # Core boundaries; no track or tunnel ends outside City Mistrzów.
    if not box(0,0,1200,1000).covers(safety):
        raise ValueError("Metro twin-rail bore extends past the city bounds")
    # Both tracks and four rails follow the same mathematical centreline.
    # Rail spacing exactly matches the existing scaled city station.
    if abs((METRO_TRACK_OFFSET*2)*K-15.2)>1e-5 or abs(GAUGE*K-1.435*K)>1e-6:
        raise ValueError("Metro track separation differs from original station")
    # One straight/switch-free two-track system, five copied prefab spans.
    station_boxes=[station_box(s) for s in METRO]
    disconnected=line
    for region in station_boxes:
        disconnected=disconnected.difference(region)
    segments=list(disconnected.geoms) if disconnected.geom_type=="MultiLineString" else [disconnected]
    if len(segments)!=6 or not all(g.geom_type=="LineString" and g.length>8 for g in segments):
        raise ValueError("Expected six joined civil tunnel segments around five source station prefabs")
    # Joined length is exact; civil tunnel cuts overlap neither nor leave
    # a gap with the station-local source tracks.
    total=sum(s.length for s in segments)+sum(s.length for s in occupied)
    if abs(total-line.length)>.1:
        raise ValueError("Tunnel connections are discontinuous or overlap source station tracks")

    return dict(status="STATIC_METRO_NETWORK_PASS_UNITY_NOT_RUN",
                stationOrder=list(EXPECTED_ORDER),
                stations=records,
                tunnelSegments=len(segments),
                twinTracks=2,railHeads=4,
                lineNominalMetres=round(line.length,2),
                cityBoreHalfWidth=round(RAIL_BORE_HALF_WIDTH,2),
                collisionExcluded=["T11 underground casino parking","T11 garage entrance ramp"],
                trainDrivingTested=False,androidTested=False,unitySceneTested=False)


def main():
    report=assess()
    out=ROOT/"mistrzowie-evidence"
    out.mkdir(parents=True,exist_ok=True)
    (out/"metro-network-five-stations.json").write_text(
        json.dumps(report,ensure_ascii=False,indent=2)+"\n")
    print(json.dumps(report,ensure_ascii=False))


if __name__=="__main__":
    main()
