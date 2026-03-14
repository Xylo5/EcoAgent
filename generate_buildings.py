"""
Generate placeholder building prefabs, materials, BuildingData assets, and meta files
for: Market, Hospital, Park, Fountain, Garden, Recycle, Farm, Water, Heritage, Gas.
"""
import os
import uuid
import random

BASE = os.path.join(os.path.dirname(os.path.abspath(__file__)), "Assets")

# BuildingData.cs script GUID (constant across all assets)
BUILDING_DATA_SCRIPT_GUID = "4102b1b6f7a4089458d6ccb9e970876f"
# URP Lit shader GUID
URP_SHADER_GUID = "933532a4fcc9baf4fa0491de14d08ed7"
# URP AssetVersion script GUID
URP_ASSET_VERSION_GUID = "d0353a89b1f911e48b9e16bdc9f2e058"

# Building definitions: (name, shortName, sizeInCells, scaleX, scaleY, scaleZ, colorR, colorG, colorB)
BUILDINGS = [
    ("Market",    "Market",    5,  4.0, 2.5, 4.0,  1.0,  0.65, 0.0  ),  # Orange
    ("Hospital",  "Hospital",  6,  5.0, 4.0, 5.0,  1.0,  0.2,  0.2  ),  # Red
    ("Park",      "Park",      4,  3.0, 0.5, 3.0,  0.2,  0.8,  0.2  ),  # Green
    ("Fountain",  "Fountain",  3,  2.0, 1.5, 2.0,  0.3,  0.7,  1.0  ),  # Light Blue
    ("Garden",    "Garden",    3,  2.5, 0.8, 2.5,  0.4,  0.9,  0.3  ),  # Lime
    ("Recycle",   "Recycle",   4,  3.0, 2.0, 3.0,  0.6,  0.85, 0.2  ),  # Yellow-Green
    ("Farm",      "Farm",      7,  6.0, 1.5, 6.0,  0.85, 0.75, 0.4  ),  # Wheat/Brown
    ("Water",     "Water",     5,  4.0, 1.0, 4.0,  0.15, 0.5,  0.95 ),  # Blue
    ("Heritage",  "Heritage",  5,  4.0, 3.5, 4.0,  0.75, 0.55, 0.35 ),  # Brown/Tan
    ("Gas",       "Gas",       4,  3.0, 2.5, 3.0,  0.9,  0.9,  0.1  ),  # Yellow
]

def make_guid():
    return uuid.uuid4().hex

def make_file_id():
    return random.randint(1000000000000000000, 9223372036854775807)

# ── Material template ──────────────────────────────────────────────
def make_material(name, r, g, b, mat_guid):
    urp_mono_id = make_file_id()
    return f"""%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!114 &-{urp_mono_id}
MonoBehaviour:
  m_ObjectHideFlags: 11
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: 0}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: {URP_ASSET_VERSION_GUID}, type: 3}}
  m_Name:
  m_EditorClassIdentifier: Unity.RenderPipelines.Universal.Editor::UnityEditor.Rendering.Universal.AssetVersion
  version: 10
--- !u!21 &2100000
Material:
  serializedVersion: 8
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_Name: {name}
  m_Shader: {{fileID: 4800000, guid: {URP_SHADER_GUID}, type: 3}}
  m_Parent: {{fileID: 0}}
  m_ModifiedSerializedProperties: 0
  m_ValidKeywords: []
  m_InvalidKeywords: []
  m_LightmapFlags: 4
  m_EnableInstancingVariants: 0
  m_DoubleSidedGI: 0
  m_CustomRenderQueue: -1
  stringTagMap:
    RenderType: Opaque
  disabledShaderPasses:
  - MOTIONVECTORS
  m_LockedProperties:
  m_SavedProperties:
    serializedVersion: 3
    m_TexEnvs:
    - _BaseMap:
        m_Texture: {{fileID: 0}}
        m_Scale: {{x: 1, y: 1}}
        m_Offset: {{x: 0, y: 0}}
    - _BumpMap:
        m_Texture: {{fileID: 0}}
        m_Scale: {{x: 1, y: 1}}
        m_Offset: {{x: 0, y: 0}}
    - _DetailAlbedoMap:
        m_Texture: {{fileID: 0}}
        m_Scale: {{x: 1, y: 1}}
        m_Offset: {{x: 0, y: 0}}
    - _DetailMask:
        m_Texture: {{fileID: 0}}
        m_Scale: {{x: 1, y: 1}}
        m_Offset: {{x: 0, y: 0}}
    - _DetailNormalMap:
        m_Texture: {{fileID: 0}}
        m_Scale: {{x: 1, y: 1}}
        m_Offset: {{x: 0, y: 0}}
    - _EmissionMap:
        m_Texture: {{fileID: 0}}
        m_Scale: {{x: 1, y: 1}}
        m_Offset: {{x: 0, y: 0}}
    - _MainTex:
        m_Texture: {{fileID: 0}}
        m_Scale: {{x: 1, y: 1}}
        m_Offset: {{x: 0, y: 0}}
    - _MetallicGlossMap:
        m_Texture: {{fileID: 0}}
        m_Scale: {{x: 1, y: 1}}
        m_Offset: {{x: 0, y: 0}}
    - _OcclusionMap:
        m_Texture: {{fileID: 0}}
        m_Scale: {{x: 1, y: 1}}
        m_Offset: {{x: 0, y: 0}}
    - _ParallaxMap:
        m_Texture: {{fileID: 0}}
        m_Scale: {{x: 1, y: 1}}
        m_Offset: {{x: 0, y: 0}}
    - _SpecGlossMap:
        m_Texture: {{fileID: 0}}
        m_Scale: {{x: 1, y: 1}}
        m_Offset: {{x: 0, y: 0}}
    - unity_Lightmaps:
        m_Texture: {{fileID: 0}}
        m_Scale: {{x: 1, y: 1}}
        m_Offset: {{x: 0, y: 0}}
    - unity_LightmapsInd:
        m_Texture: {{fileID: 0}}
        m_Scale: {{x: 1, y: 1}}
        m_Offset: {{x: 0, y: 0}}
    - unity_ShadowMasks:
        m_Texture: {{fileID: 0}}
        m_Scale: {{x: 1, y: 1}}
        m_Offset: {{x: 0, y: 0}}
    m_Ints: []
    m_Floats:
    - _AddPrecomputedVelocity: 0
    - _AlphaClip: 0
    - _AlphaToMask: 0
    - _Blend: 0
    - _BlendModePreserveSpecular: 1
    - _BumpScale: 1
    - _ClearCoatMask: 0
    - _ClearCoatSmoothness: 0
    - _Cull: 2
    - _Cutoff: 0.5
    - _DetailAlbedoMapScale: 1
    - _DetailNormalMapScale: 1
    - _DstBlend: 0
    - _DstBlendAlpha: 0
    - _EnvironmentReflections: 1
    - _GlossMapScale: 0
    - _Glossiness: 0
    - _GlossyReflections: 0
    - _Metallic: 0
    - _OcclusionStrength: 1
    - _Parallax: 0.005
    - _QueueOffset: 0
    - _ReceiveShadows: 1
    - _Smoothness: 0.5
    - _SmoothnessTextureChannel: 0
    - _SpecularHighlights: 1
    - _SrcBlend: 1
    - _SrcBlendAlpha: 1
    - _Surface: 0
    - _WorkflowMode: 1
    - _XRMotionVectorsPass: 1
    - _ZWrite: 1
    m_Colors:
    - _BaseColor: {{r: {r}, g: {g}, b: {b}, a: 1}}
    - _Color: {{r: {r}, g: {g}, b: {b}, a: 1}}
    - _EmissionColor: {{r: 0, g: 0, b: 0, a: 1}}
    - _SpecColor: {{r: 0.19999996, g: 0.19999996, b: 0.19999996, a: 1}}
  m_BuildTextureStacks: []
  m_AllowLocking: 1
"""

# ── Prefab template ────────────────────────────────────────────────
def make_prefab(name, sx, sy, sz, mat_guid):
    go_id = make_file_id()
    tr_id = make_file_id()
    mf_id = make_file_id()
    mr_id = make_file_id()
    bc_id = make_file_id()
    content = f"""%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!1 &{go_id}
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  serializedVersion: 6
  m_Component:
  - component: {{fileID: {tr_id}}}
  - component: {{fileID: {mf_id}}}
  - component: {{fileID: {mr_id}}}
  - component: {{fileID: {bc_id}}}
  m_Layer: 0
  m_Name: {name}
  m_TagString: Untagged
  m_Icon: {{fileID: 0}}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
--- !u!4 &{tr_id}
Transform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {go_id}}}
  serializedVersion: 2
  m_LocalRotation: {{x: 0, y: 0, z: 0, w: 1}}
  m_LocalPosition: {{x: 0, y: 0, z: 0}}
  m_LocalScale: {{x: {sx}, y: {sy}, z: {sz}}}
  m_ConstrainProportionsScale: 0
  m_Children: []
  m_Father: {{fileID: 0}}
  m_LocalEulerAnglesHint: {{x: 0, y: 0, z: 0}}
--- !u!33 &{mf_id}
MeshFilter:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {go_id}}}
  m_Mesh: {{fileID: 10202, guid: 0000000000000000e000000000000000, type: 0}}
--- !u!23 &{mr_id}
MeshRenderer:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {go_id}}}
  m_Enabled: 1
  m_CastShadows: 1
  m_ReceiveShadows: 1
  m_DynamicOccludee: 1
  m_StaticShadowCaster: 0
  m_MotionVectors: 1
  m_LightProbeUsage: 1
  m_ReflectionProbeUsage: 1
  m_RayTracingMode: 2
  m_RayTraceProcedural: 0
  m_RayTracingAccelStructBuildFlagsOverride: 0
  m_RayTracingAccelStructBuildFlags: 1
  m_SmallMeshCulling: 1
  m_ForceMeshLod: -1
  m_MeshLodSelectionBias: 0
  m_RenderingLayerMask: 1
  m_RendererPriority: 0
  m_Materials:
  - {{fileID: 2100000, guid: {mat_guid}, type: 2}}
  m_StaticBatchInfo:
    firstSubMesh: 0
    subMeshCount: 0
  m_StaticBatchRoot: {{fileID: 0}}
  m_ProbeAnchor: {{fileID: 0}}
  m_LightProbeVolumeOverride: {{fileID: 0}}
  m_ScaleInLightmap: 1
  m_ReceiveGI: 1
  m_PreserveUVs: 0
  m_IgnoreNormalsForChartDetection: 0
  m_ImportantGI: 0
  m_StitchLightmapSeams: 1
  m_SelectedEditorRenderState: 3
  m_MinimumChartSize: 4
  m_AutoUVMaxDistance: 0.5
  m_AutoUVMaxAngle: 89
  m_LightmapParameters: {{fileID: 0}}
  m_GlobalIlluminationMeshLod: 0
  m_SortingLayerID: 0
  m_SortingLayer: 0
  m_SortingOrder: 0
  m_MaskInteraction: 0
  m_AdditionalVertexStreams: {{fileID: 0}}
--- !u!65 &{bc_id}
BoxCollider:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {go_id}}}
  m_Material: {{fileID: 0}}
  m_IncludeLayers:
    serializedVersion: 2
    m_Bits: 0
  m_ExcludeLayers:
    serializedVersion: 2
    m_Bits: 0
  m_LayerOverridePriority: 0
  m_IsTrigger: 0
  m_ProvidesContacts: 0
  m_Enabled: 1
  serializedVersion: 3
  m_Size: {{x: 1, y: 1, z: 1}}
  m_Center: {{x: 0, y: 0, z: 0}}
"""
    return content, go_id

# ── BuildingData .asset template ───────────────────────────────────
def make_building_data(asset_name, building_name, size, prefab_guid, prefab_root_id):
    return f"""%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!114 &11400000
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: 0}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: {BUILDING_DATA_SCRIPT_GUID}, type: 3}}
  m_Name: {asset_name}
  m_EditorClassIdentifier: Assembly-CSharp::BuildingData
  buildingName: {building_name}
  icon: {{fileID: 0}}
  sizeInCells: {size}
  prefab: {{fileID: {prefab_root_id}, guid: {prefab_guid}, type: 3}}
  validColor: {{r: 0, g: 1, b: 0, a: 0.5}}
  invalidColor: {{r: 1, g: 0, b: 0, a: 0.5}}
"""

# ── Meta file templates ────────────────────────────────────────────
def make_prefab_meta(guid):
    return f"""fileFormatVersion: 2
guid: {guid}
PrefabImporter:
  externalObjects: {{}}
  userData:
  assetBundleName:
  assetBundleVariant:
"""

def make_native_meta(guid, main_object_id):
    return f"""fileFormatVersion: 2
guid: {guid}
NativeFormatImporter:
  externalObjects: {{}}
  mainObjectFileID: {main_object_id}
  userData:
  assetBundleName:
  assetBundleVariant:
"""

# ── Generate everything ────────────────────────────────────────────
def main():
    mat_dir = os.path.join(BASE, "Materials")
    prefab_dir = os.path.join(BASE, "Prefabs")

    os.makedirs(mat_dir, exist_ok=True)
    os.makedirs(prefab_dir, exist_ok=True)

    created = []

    for (name, short, size, sx, sy, sz, r, g, b) in BUILDINGS:
        # 1. Material
        mat_guid = make_guid()
        mat_name = f"Mat_{name}"
        mat_path = os.path.join(mat_dir, f"{mat_name}.mat")
        mat_meta_path = mat_path + ".meta"

        with open(mat_path, "w", newline="\n") as f:
            f.write(make_material(mat_name, r, g, b, mat_guid))
        with open(mat_meta_path, "w", newline="\n") as f:
            f.write(make_native_meta(mat_guid, 2100000))

        # 2. Prefab
        prefab_guid = make_guid()
        prefab_name = f"PH-{name}"
        prefab_path = os.path.join(prefab_dir, f"{prefab_name}.prefab")
        prefab_meta_path = prefab_path + ".meta"

        prefab_content, prefab_root_id = make_prefab(prefab_name, sx, sy, sz, mat_guid)
        with open(prefab_path, "w", newline="\n") as f:
            f.write(prefab_content)
        with open(prefab_meta_path, "w", newline="\n") as f:
            f.write(make_prefab_meta(prefab_guid))

        # 3. BuildingData asset
        asset_guid = make_guid()
        asset_path = os.path.join(BASE, f"{name}.asset")
        asset_meta_path = asset_path + ".meta"

        with open(asset_path, "w", newline="\n") as f:
            f.write(make_building_data(name, name, size, prefab_guid, prefab_root_id))
        with open(asset_meta_path, "w", newline="\n") as f:
            f.write(make_native_meta(asset_guid, 11400000))

        created.append(name)
        print(f"Created: {mat_name}.mat  |  {prefab_name}.prefab  |  {name}.asset")

    # Also create the Materials folder .meta if missing
    mat_dir_meta = mat_dir + ".meta"
    if not os.path.exists(mat_dir_meta):
        folder_guid = make_guid()
        with open(mat_dir_meta, "w", newline="\n") as f:
            f.write(f"""fileFormatVersion: 2
guid: {folder_guid}
folderAsset: yes
DefaultImporter:
  externalObjects: {{}}
  userData:
  assetBundleName:
  assetBundleVariant:
""")
        print(f"Created: Materials.meta")

    print(f"\nDone! Created assets for: {', '.join(created)}")

if __name__ == "__main__":
    main()
