#!/usr/bin/env python3
"""Write native Unity material assets and a playable entry scene, without running Unity."""
from pathlib import Path
import re
import uuid

ROOT = Path(__file__).resolve().parents[1] / "SkyRacers"
ASSETS = ROOT / "Assets"


def guid(path, importer="DefaultImporter", settings=""):
    meta = Path(str(path) + ".meta")
    if meta.exists():
        return re.search(r"guid: ([0-9a-f]+)", meta.read_text())[1]
    value = uuid.uuid5(uuid.NAMESPACE_URL, "skyracers/" + str(path.relative_to(ASSETS))).hex
    folder = "folderAsset: yes\n" if path.is_dir() else ""
    meta.write_text(f"fileFormatVersion: 2\nguid: {value}\n{folder}{importer}:\n  externalObjects: {{}}\n{settings}  userData: \n  assetBundleName: \n  assetBundleVariant: \n")
    return value


def material(name, shader, color, texture=None):
    path = ASSETS / "Game/Materials" / (name + ".mat")
    path.parent.mkdir(parents=True, exist_ok=True)
    tex = "{fileID: 0}" if texture is None else "{fileID: 2800000, guid: " + texture + ", type: 3}"
    keywords = "\n  - _ALPHATEST_ON" if texture else " []"
    path.write_text(f'''%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!21 &2100000
Material:
  serializedVersion: 8
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_Name: {name}
  m_Shader: {{fileID: 4800000, guid: {shader}, type: 3}}
  m_Parent: {{fileID: 0}}
  m_ModifiedSerializedProperties: 0
  m_ValidKeywords:{keywords}
  m_InvalidKeywords: []
  m_LightmapFlags: 4
  m_EnableInstancingVariants: 0
  m_DoubleSidedGI: 1
  m_CustomRenderQueue: -1
  stringTagMap: {{}}
  disabledShaderPasses: []
  m_LockedProperties:
  m_SavedProperties:
    serializedVersion: 3
    m_TexEnvs:
    - _BaseMap:
        m_Texture: {tex}
        m_Scale: {{x: 1, y: 1}}
        m_Offset: {{x: 0, y: 0}}
    m_Ints: []
    m_Floats:
    - _AlphaClip: {1 if texture else 0}
    - _Cull: {0 if texture else 2}
    - _Cutoff: 0.05
    - _Smoothness: 0.22
    - _Metallic: 0
    - _Surface: 0
    - _ZWrite: 1
    - _SrcBlend: 1
    - _DstBlend: 0
    m_Colors:
    - _BaseColor: {{r: {color[0]}, g: {color[1]}, b: {color[2]}, a: 1}}
    - _EmissionColor: {{r: 0, g: 0, b: 0, a: 1}}
  m_BuildTextureStacks: []
''')
    return "{fileID: 2100000, guid: " + guid(path, "NativeFormatImporter", "  mainObjectFileID: 2100000\n") + ", type: 2}"


def main():
    package = next((ROOT / "Library/PackageCache").glob("com.unity.render-pipelines.universal@*"))
    shader = lambda name: re.search(r"guid: (\w+)", (package / f"Shaders/{name}.shader.meta").read_text())[1]
    materials = []
    for i in range(9):
        texture = ASSETS / f"ThirdParty/F18/Texture_{i}.png"
        texguid = guid(texture, "TextureImporter", "  serializedVersion: 12\n  mipmaps:\n    enableMipMap: 1\n    sRGBTexture: 1\n  textureSettings:\n    serializedVersion: 2\n    filterMode: 0\n    aniso: 1\n    mipBias: 0\n    wrapU: 0\n    wrapV: 0\n    wrapW: 0\n  alphaIsTransparency: 1\n  textureType: 0\n  textureShape: 1\n")
        materials.append(material("F18_" + str(i), shader("Lit"), (1, 1, 1), texguid))
    rock = material("Canyon Rock", guid(ASSETS / "Shaders/CanyonRock.shader", "ShaderImporter"), (1, 1, 1))
    marker = material("Course markers", shader("Lit"), (.85, .96, .35))
    dark = material("Beacon steel", shader("Lit"), (.1, .14, .14))
    exhaust = material("Engine glow", guid(ASSETS / "Shaders/Exhaust.shader", "ShaderImporter"), (.22, .55, 2))
    script = guid(ASSETS / "Scripts/GameBootstrap.cs", "MonoImporter", "  serializedVersion: 2\n  defaultReferences: []\n  executionOrder: 0\n  icon: {instanceID: 0}\n")
    data = guid(ASSETS / "ThirdParty/F18/aircraft.json", "TextScriptImporter")
    sample = (ASSETS / "Scenes/SampleScene.unity").read_text()
    header = sample[:sample.index("--- !u!1 &")]
    scene = ASSETS / "Scenes/CanyonRun.unity"
    scene.write_text(header + f'''--- !u!1 &1000
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  serializedVersion: 6
  m_Component:
  - component: {{fileID: 1001}}
  - component: {{fileID: 1002}}
  m_Layer: 0
  m_Name: SkyRacers - Play to Start
  m_TagString: Untagged
  m_Icon: {{fileID: 0}}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
--- !u!4 &1001
Transform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: 1000}}
  serializedVersion: 2
  m_LocalRotation: {{x: 0, y: 0, z: 0, w: 1}}
  m_LocalPosition: {{x: 0, y: 0, z: 0}}
  m_LocalScale: {{x: 1, y: 1, z: 1}}
  m_ConstrainProportionsScale: 0
  m_Children: []
  m_Father: {{fileID: 0}}
  m_LocalEulerAnglesHint: {{x: 0, y: 0, z: 0}}
--- !u!114 &1002
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: 1000}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: {script}, type: 3}}
  m_Name:
  m_EditorClassIdentifier:
  aircraftData: {{fileID: 4900000, guid: {data}, type: 3}}
  aircraftMaterials:
''' + ''.join('  - ' + m + '\n' for m in materials) + f'''  terrainMaterial: {rock}
  markerMaterial: {marker}
  darkMaterial: {dark}
  exhaustMaterial: {exhaust}
--- !u!1660057539 &9223372036854775807
SceneRoots:
  m_ObjectHideFlags: 0
  m_Roots:
  - {{fileID: 1001}}
''')
    sceneguid = guid(scene)
    settings = ROOT / "ProjectSettings/EditorBuildSettings.asset"
    text = settings.read_text()
    text = re.sub(r"  m_Scenes:.*?  m_configObjects:", f"  m_Scenes:\n  - enabled: 1\n    path: Assets/Scenes/CanyonRun.unity\n    guid: {sceneguid}\n  m_configObjects:", text, flags=re.S)
    settings.write_text(text)
    for path in sorted(ASSETS.rglob("*")):
        if path.suffix == ".meta": continue
        importer = "MonoImporter" if path.suffix == ".cs" else "TextScriptImporter" if path.suffix == ".json" else "DefaultImporter"
        guid(path, importer)
    print("Playable scene and all referenced materials/meta files created.")


if __name__ == "__main__":
    main()
