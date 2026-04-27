using UnityEngine;

namespace MCGame.Core
{
    /// <summary>
    /// Named accessors for project physics layers.
    /// Replaces magic numbers like (1 &lt;&lt; 13) that break silently when project layer
    /// configuration changes.
    ///
    /// Layer indices are read from Unity's TagManager via LayerMask.NameToLayer() on
    /// first access. If a layer is renamed in Project Settings, update the string here.
    /// If a layer doesn't exist, accessor returns -1 and a one-time warning logs.
    ///
    /// Usage:
    ///   LayerMask mask = Layers.NPCMask;
    ///   int layer = Layers.NPC;
    ///   if (gameObject.layer == Layers.Building) { ... }
    /// </summary>
    public static class Layers
    {
        // Layer name constants — match Project Settings → Tags and Layers
        private const string NAME_DEFAULT = "Default";
        private const string NAME_GROUND = "Ground";
        private const string NAME_ROAD = "Road";
        private const string NAME_BUILDING = "Building";
        private const string NAME_INTERACTABLE = "Interactable";
        private const string NAME_VEHICLE = "Vehicle";
        private const string NAME_NPC = "NPC";
        private const string NAME_TRIGGER = "Trigger";
        private const string NAME_MINIMAP = "Minimap";

        // Lazy-evaluated layer indices (sentinel value -2 means "not yet computed")
        private static int _default = -2;
        private static int _ground = -2;
        private static int _road = -2;
        private static int _building = -2;
        private static int _interactable = -2;
        private static int _vehicle = -2;
        private static int _npc = -2;
        private static int _trigger = -2;
        private static int _minimap = -2;

        // ----- Layer indices -----
        public static int Default => GetLayer(ref _default, NAME_DEFAULT);
        public static int Ground => GetLayer(ref _ground, NAME_GROUND);
        public static int Road => GetLayer(ref _road, NAME_ROAD);
        public static int Building => GetLayer(ref _building, NAME_BUILDING);
        public static int Interactable => GetLayer(ref _interactable, NAME_INTERACTABLE);
        public static int Vehicle => GetLayer(ref _vehicle, NAME_VEHICLE);
        public static int NPC => GetLayer(ref _npc, NAME_NPC);
        public static int Trigger => GetLayer(ref _trigger, NAME_TRIGGER);
        public static int Minimap => GetLayer(ref _minimap, NAME_MINIMAP);

        // ----- LayerMasks (single-bit) -----
        public static LayerMask DefaultMask => 1 << Default;
        public static LayerMask GroundMask => 1 << Ground;
        public static LayerMask RoadMask => 1 << Road;
        public static LayerMask BuildingMask => 1 << Building;
        public static LayerMask InteractableMask => 1 << Interactable;
        public static LayerMask VehicleMask => 1 << Vehicle;
        public static LayerMask NPCMask => 1 << NPC;
        public static LayerMask TriggerMask => 1 << Trigger;
        public static LayerMask MinimapMask => 1 << Minimap;

        // ----- Common composite masks -----
        /// <summary>
        /// Layers that block raycasts for line-of-sight (witness checks, AI vision, etc.)
        /// Currently: Default, Ground, Building.
        /// </summary>
        public static LayerMask LineOfSightObstructionMask =>
            DefaultMask | GroundMask | BuildingMask;

#if UNITY_EDITOR
        [UnityEditor.InitializeOnEnterPlayMode]
        private static void ResetCacheOnEnterPlayMode()
        {
            // Clear cached layer values so domain reload changes are picked up.
            _default = _ground = _road = _building = -2;
            _interactable = _vehicle = _npc = _trigger = _minimap = -2;
        }
#endif

        private static int GetLayer(ref int cache, string layerName)
        {
            if (cache == -2)
            {
                cache = LayerMask.NameToLayer(layerName);
                if (cache == -1)
                {
                    Debug.LogWarning(
                        $"[Layers] Layer '{layerName}' not found in Project Settings. " +
                        $"Returning -1. Verify Tags and Layers configuration.");
                }
            }
            return cache;
        }
    }
}