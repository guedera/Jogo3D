#!/usr/bin/env python3
"""Convert this project's static glTF hierarchy to an offline Unity import payload.

No third-party packages required. Keeps per-part pivots, UVs and materials.
The original glTF stays in ThirdParty for provenance; runtime uses a baked prefab.
"""
import base64
import json
import struct
import zipfile
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / "SkyRacers/Assets/ThirdParty/F18"


def convert():
    OUT.mkdir(parents=True, exist_ok=True)
    with zipfile.ZipFile(ROOT / "low-poly-f18-super-hornet.zip") as archive:
        source = archive.read("source/model.gltf")
    (OUT / "model.gltf").write_bytes(source)
    gltf = json.loads(source)
    buffers = [base64.b64decode(b["uri"].split(",", 1)[1]) for b in gltf["buffers"]]

    def accessor(index):
        a = gltf["accessors"][index]
        view = gltf["bufferViews"][a["bufferView"]]
        count = {"SCALAR": 1, "VEC2": 2, "VEC3": 3, "VEC4": 4}[a["type"]]
        fmt = "<" + {5126: "f", 5125: "I", 5123: "H", 5121: "B"}[a["componentType"]] * count
        stride = view.get("byteStride", struct.calcsize(fmt))
        offset = view.get("byteOffset", 0) + a.get("byteOffset", 0)
        return [list(struct.unpack_from(fmt, buffers[view["buffer"]], offset + i * stride))
                for i in range(a["count"])]

    def vec(v):
        return dict(zip(("x", "y", "z"), (v[0], v[1], -v[2])))

    def quat(v):
        return dict(zip(("x", "y", "z", "w"), (-v[0], -v[1], v[2], v[3])))

    parents = {child: i for i, n in enumerate(gltf["nodes"]) for child in n.get("children", [])}
    # The demo animation retracts the landing gear and opens the wings.
    # Bake only gear/door final poses; never play the demonstration in flight.
    gear = {53, 55, 57, 64, 66, 68, 70, 72, 74, 76, 78, 83, 88}
    posed = [dict(n) for n in gltf["nodes"]]
    for anim in gltf.get("animations", []):
        for channel in anim["channels"]:
            index = channel["target"]["node"]
            if index in gear:
                sampler = anim["samplers"][channel["sampler"]]
                posed[index][channel["target"]["path"]] = accessor(sampler["output"])[-1]

    nodes = []
    for i, n in enumerate(posed):
        node = {"name": n.get("name", "Root"), "parent": parents.get(i, -1),
                "position": vec(n.get("translation", [0, 0, 0])),
                "rotation": quat(n.get("rotation", [0, 0, 0, 1])),
                "scale": dict(zip(("x", "y", "z"), n.get("scale", [1, 1, 1]))),
                "parts": []}
        if "mesh" in n:
            for p in gltf["meshes"][n["mesh"]]["primitives"]:
                assert p.get("mode", 4) == 4, "Only triangles supported"
                attrs = p["attributes"]
                vertices = [vec(v) for v in accessor(attrs["POSITION"])]
                normals = [vec(v) for v in accessor(attrs["NORMAL"])] if "NORMAL" in attrs else []
                uv = [{"x": v[0], "y": 1 - v[1]} for v in accessor(attrs["TEXCOORD_0"])]
                indices = [v[0] for v in accessor(p["indices"])]
                for t in range(0, len(indices), 3):
                    indices[t+1], indices[t+2] = indices[t+2], indices[t+1]
                node["parts"].append({"vertices": vertices, "normals": normals, "uv": uv,
                                      "triangles": indices, "material": p.get("material", 0)})
        nodes.append(node)
    for i, material in enumerate(gltf["materials"]):
        texture = material["pbrMetallicRoughness"]["baseColorTexture"]["index"]
        image = gltf["images"][gltf["textures"][texture]["source"]]
        (OUT / f"Texture_{i}.png").write_bytes(base64.b64decode(image["uri"].split(",", 1)[1]))
    (OUT / "aircraft.json").write_text(json.dumps({"nodes": nodes, "materialCount": len(gltf["materials"])},
                                                separators=(",", ":")))
    print(f"Converted {len(nodes)} nodes; {sum(len(n['parts']) for n in nodes)} primitives; gear retracted.")


if __name__ == "__main__":
    convert()
