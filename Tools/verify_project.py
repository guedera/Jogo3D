#!/usr/bin/env python3
"""Check imported model data / scene references and compile against installed Unity assemblies.

Run after Unity has resolved packages once. This does not replace a Play Mode test.
"""
import json
import math
import re
import subprocess
import tempfile
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1] / "SkyRacers"


def validate_data():
    model = json.loads((ROOT / "Assets/ThirdParty/F18/aircraft.json").read_text())
    triangles = 0
    for node in model["nodes"]:
        for part in node["parts"]:
            assert len(part["triangles"]) % 3 == 0
            assert all(0 <= index < len(part["vertices"]) for index in part["triangles"])
            assert len(part["uv"]) == len(part["vertices"])
            triangles += len(part["triangles"]) // 3
    for index in (15, 31):
        assert model["nodes"][index]["rotation"] == dict(x=0, y=0, z=0, w=1), "Wing fold must remain neutral"
    assert triangles == 5351
    # Validate the hinge coordinates actually consumed by AircraftFactory against
    # vertices in the supplied asset, rather than assuming a cuboid's origin is a hinge.
    factory = (ROOT / "Assets/Scripts/AircraftFactory.cs").read_text()
    hinges = re.findall(r"AddHingedSurface\(mapped, nodes\[(\d+)\], new Vector3\(([^)]+)\), new Vector3\(([^)]+)\), FlightSurfaceRole\.(\w+), (-?\d+)\)", factory)
    assert len(hinges) == 6, "Expected two ailerons, two flaps and two rudders"
    assert len({int(h[0]) for h in hinges}) == 6, "Duplicate surface mesh"
    for index, first, second, role, side in hinges:
        assert int(index) not in (15, 31), "Wing fold must never be animated"
        vertices = [tuple(v[a] for a in "xyz") for p in model["nodes"][int(index)]["parts"] for v in p["vertices"]]
        ends = [tuple(float(x.strip().rstrip("f")) for x in point.split(",")) for point in (first, second)]
        for point in ends:
            assert min(math.dist(point, v) for v in vertices) < .015, (index, "hinge misses surface edge")
        direction = 1 if role == "Rudder" else 0
        assert ends[1][direction] > ends[0][direction], (index, "hinge axis reversed")
    print("PASS: six surface hinges lie on asset edges; axis directions and fixed wing folds")
    known = set()
    for directory in (ROOT / "Assets", ROOT / "Library/PackageCache"):
        for meta in directory.rglob("*.meta"):
            match = re.search(r"^guid: ([0-9a-f]{32})", meta.read_text(errors="ignore"), re.M)
            if match:
                known.add(match[1])
    files = [ROOT / "Assets/Scenes/CanyonRun.unity", *(ROOT / "Assets/Game/Materials").glob("*.mat")]
    for file in files:
        for guid in re.findall(r"guid: ([0-9a-f]{32})", file.read_text()):
            assert guid in known or guid.startswith("0000000000000000"), (file, guid)
    print("PASS: aircraft buffers, unfolded wings, scene and material GUID references")


def compile_scripts():
    version = re.search(r"m_EditorVersion: (\S+)", (ROOT / "ProjectSettings/ProjectVersion.txt").read_text())[1]
    scripting = Path(f"/Applications/Unity/Hub/Editor/{version}/Unity.app/Contents/Resources/Scripting")
    compiler = next((scripting / "DotNetSdk/sdk").glob("*/Roslyn/bincore/csc.dll"))
    response = next((ROOT / "Library/Bee/artifacts").glob("*/Assembly-CSharp.rsp"))
    common = [line for line in response.read_text().splitlines()
              if not line.startswith(("-out:", "-refout:", "-analyzer:", "/additionalfile:", '"Assets/'))]
    with tempfile.TemporaryDirectory(prefix="skyracers-check-") as temporary:
        directory = Path(temporary)
        runtime = directory / "SkyRacersCheck.dll"
        for folder, output, extra in [("Scripts", runtime, []), ("Editor", directory / "SkyRacersEditorCheck.dll", [f'-r:"{runtime}"'])]:
            response_file = directory / (folder + ".rsp")
            args = common + extra + [f'-out:"{output}"']
            args += [f'"{path.relative_to(ROOT)}"' for path in (ROOT / "Assets" / folder).glob("*.cs")]
            response_file.write_text("\n".join(args))
            subprocess.run([str(scripting / "NetCoreRuntime/dotnet"), str(compiler), "@" + str(response_file)], cwd=ROOT, check=True)
            print("PASS: C# compilation — " + folder)

        # Execute the real scalar flight equations and HUD calculations in .NET.
        # Unity physics/rendering still require a separate Play Mode test.
        runtime_root = scripting / "NetCoreRuntime"
        framework = next((runtime_root / "shared/Microsoft.NETCore.App").iterdir())
        output = directory / "FlightChecks.dll"
        response_file = directory / "FlightChecks.rsp"
        args = ["-nologo", "-target:exe", "-nostdlib+", f'-out:"{output}"']
        args += [f'-r:"{path}"' for path in framework.glob("*.dll")]
        args += [f'"{ROOT / "Assets/Scripts" / name}"' for name in ("FlightDynamics.cs", "HudTape.cs", "FlightSurfaceMixer.cs")]
        args += [f'"{ROOT.parent / "Tools/FlightChecks.cs"}"']
        response_file.write_text("\n".join(args))
        subprocess.run([str(runtime_root / "dotnet"), str(compiler), "@" + str(response_file)], check=True)
        output.with_suffix(".runtimeconfig.json").write_text(json.dumps({"runtimeOptions": {
            "tfm": "net8.0", "framework": {"name": "Microsoft.NETCore.App", "version": framework.name}}}))
        subprocess.run([str(runtime_root / "dotnet"), str(output)], check=True)


if __name__ == "__main__":
    validate_data()
    compile_scripts()
