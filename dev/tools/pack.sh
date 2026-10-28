#!/bin/bash
# gltfpack all environment / prop / car assets for the street build
cd /home/claude/work
GP=./bin/gltfpack
O=assets/src
A=att
p() { out=$1; shift; echo "== $out"; $GP "$@" -o $O/$out.glb -cc 2>&1 | grep -iv "^warning" | tail -2; ls -la $O/$out.glb; }
p row   -i up/rowhouses_on_29th_st__baltimore.glb -si 0.12 -tc -tq 9 -tl 4096
p fire  -i up/small_fire_at_local_business_photogrammetry.glb -si 0.12 -tc -tq 9 -tl 4096
p bridge -i up/rick_roll_bridge.glb -si 0.08 -tc -tq 9 -tl 4096
p crypt -i up/lincolns_inn_chapel_undercroft_1.glb -si 0.07 -tc -tq 9 -tl 4096
p nyc   -i up/street_1.glb -si 0.03 -tc -tq 9 -tl 4096
p lot   -i up/trump_village.glb -si 0.1 -tc -tq 8 -tl 2048
p city  -i street_in/1dbc1c38-street_city_7_for_games_free.glb -si 0.6 -tc -tl 1024
p lamp  -i street_in/b0e1a6bd-street_light_1.glb -tc -tl 512
p poles -i street_in/fa29ded3-street_light.glb -si 0.5 -tc -tl 512
p oct   -i $A/1f1b6c00-ufc_octagon.glb -tc -tl 2048
p people -i $A/40e4eb17-wip_-_lowpoly_people_-_2.glb -tc -tl 1024 -kn
p girl  -i $A/e4344299-character_people_girl_001.glb -si 0.25 -tc -tl 2048
p table -i $A/6e69c1e4-wooden_table_01_4k.glb -tc -tl 1024
p car_s4   -i $A/a694e239-2010_audi_s4.glb -si 0.15 -tc -tl 1024 -kn
p car_r8   -i $A/c3d7884d-2010_audi_r8_5.2_fsi_quattro.glb -si 0.12 -tc -tl 1024 -kn
p car_rev  -i $A/56ba2920-2027_lamborghini_revuelto_sv.glb -si 0.08 -tc -tl 1024 -kn
p car_918  -i $A/a4261de1-2026_porsche_918_spyder_asphalt_edition.glb -si 0.25 -tc -tl 1024 -kn
p car_mus  -i $A/7e311091-1996_jack_roush_edition_ford_mustang_gt.glb -si 0.12 -tc -tl 1024 -kn
p car_dc   -i $A/b1125667-2015_dodge_challenger_lbperformance.glb -si 0.25 -tc -tl 1024 -kn
p car_dia  -i $A/5a597f7a-1995_lamborghini_diablo_sv_-_police_interceptor.glb -si 0.8 -tc -tl 1024 -kn
p car_mar  -i $A/b701556f-2012_marussia_b2_-_patrol.glb -si 0.3 -tc -tl 1024 -kn
p car_zuk  -i $A/11fcfb1c-free_zuk_3d_model.glb -tc -tl 1024 -kn
p car_gmc  -i "tmp_gmc/source/2007 GMC Sierra.glb" -tc -tl 1024 -kn
echo DONE
