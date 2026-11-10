# Promenade + harbour preview (no Unity)

Owner 10.11.2026: "Masz przeanalizować wszystko i potem zrobić całą promenadę" - port, jachty, skutery, barierki,
budki z jedzeniem, drzewa, plaża. Renders the waterfront from the real generators before any build.

    ./run_preview.sh /tmp/promwork <path to node_modules/three>

* foundation: `legacy_foundation.py` (promenade paving PavingStones126A, palm beds, cycle lane, beach, quays,
  pontoons with dark floats, the promenade quay wall, sea)
* `BraxChampionsHarbour` (owner's yacht / Riva / jetski GLBs alongside their fingers, fittings, gates, 3D numbers),
  `BraxChampionsHarbourSafety` (railings), `BraxChampionsPromenadeFurniture` (beds, benches, lamps, boardwalks),
  `BraxChampionsPromenade` + `BraxChampionsPromenadeEquipment` (K1-K12 kiosks, kebab rotisserie, pizza oven, gelato...),
  `BraxChampionsBeachFurnishings`
* not drawn: the owner's encrypted packs (city palms, shop food models) - their places stay empty in the preview.
