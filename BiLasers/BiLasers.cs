using Elements.Core;
using FrooxEngine;
using HarmonyLib;
using Renderite.Shared;
using ResoniteModLoader;
using ProtoFluxBindings;
using ProtoFlux.Core;
using System;
using FrooxEngine.ProtoFlux.Runtimes.Execution.Nodes.Operators;
using FrooxEngine.FrooxEngine.ProtoFlux.CoreNodes;
using FrooxEngine.ProtoFlux.Runtimes.Execution.Nodes.Color;

namespace BiLasers
{
    public class Patch : ResoniteMod
    {
        public override string Name => "BiLasers";
        public override string Author => "Nexis";
        public override string Link => "https://github.com/l79627550-dot/BiLasers";
        public override string Version => "1.1.0";

        #region Mod Config Keys

        // Defaults
        static readonly bool DefaultEnabled = true;
        static readonly float DefaultSmoothSpeed = 10;
        static readonly colorX DefaultStartColor = new colorX(0.84f, 0.01f, 0.44f, 1f);
        static readonly colorX DefaultEndColor = new colorX(0f, 0.22f, 0.66f, 1f);

        // Config objects
        public static ModConfiguration? config;

        [AutoRegisterConfigKey]
        private static readonly ModConfigurationKey<bool> ENABLED = new ModConfigurationKey<bool>("enabled", "Enabled", () => DefaultEnabled);

        [AutoRegisterConfigKey]
        private static readonly ModConfigurationKey<float> SMOOTH = new ModConfigurationKey<float>("Smoothing Value", "How fast do you want the laser to change color?", () => DefaultSmoothSpeed);

        [AutoRegisterConfigKey]
        private static readonly ModConfigurationKey<colorX> START = new ModConfigurationKey<colorX>("Start Color", "Start Color:", () => DefaultStartColor);

        [AutoRegisterConfigKey]
        private static readonly ModConfigurationKey<colorX> END = new ModConfigurationKey<colorX>("End Color", "End Color", () => DefaultEndColor);


        // Variable references. replaces config.GetValue([key]), is never null
        static bool Enabled => GetConfigValue(ENABLED, DefaultEnabled);
        static float SmoothSpeed => GetConfigValue(SMOOTH, DefaultSmoothSpeed);
        static colorX StartColor => GetConfigValue(START, DefaultStartColor);
        static colorX EndColor => GetConfigValue(END, DefaultEndColor);

        #endregion

        // Dynamic Variable Names. {0}='L'/'R', {1}='Start'/'End'
        static readonly string BaseColorVariable = "User/InteractionLaser_{0}_{1}";
        static readonly string NewColorVariable = "User/Laser_{0}_{1}";

        public override void OnEngineInit()
        {
            config = GetConfiguration();

            if (config != null)
            {
                config.OnThisConfigurationChanged += OnConfigChange;
                config.Save(true);
            }
            Harmony harmony = new Harmony("com.Nexis.BiLasers");
            harmony.PatchAll();
        }
        static void OnConfigChange(ConfigurationChangedEvent configurationChangedEvent)
        {
            for (int i = 0; i < currentLasers.Count; i++)
            {
                LaserData thisLaser = currentLasers[i];

                // If the component is gone, by nulling, disposing or destroying
                if (thisLaser.laser == null || thisLaser.laser.IsRemoved)
                {
                    // This laser no longer exists, therefore remove from the list and skip to the next element
                    currentLasers.RemoveAt(i);
                    i--;
                    continue;
                }

                // For some reason this is required, otherwise the function crashes when trying to set the values
                thisLaser.laser.RunInUpdates(3, () =>
                {
                    thisLaser.startSmooth.Speed.Value = SmoothSpeed;
                    thisLaser.endSmooth.Speed.Value = SmoothSpeed;

                    thisLaser.newStartColor.Value = StartColor;
                    thisLaser.newEndColor.Value = EndColor;

                    thisLaser.laserMesh.Target = Enabled ? thisLaser.newMesh : thisLaser.originalMesh;
                });
            }
        }

        [HarmonyPatch(typeof(InteractionLaser))]
        class InteractionLaserPatch
        {
            [HarmonyPrefix]
            [HarmonyPatch("OnAwake")]
            public static void Prefix(InteractionLaser __instance,
                               FieldDrive<colorX> ____startColor,
                               FieldDrive<colorX> ____endColor,
                               FieldDrive<float3> ____directPoint,
                               FieldDrive<float3> ____actualPoint,
                               SyncRef<InteractionHandler> ____handler)
            {
                __instance.RunInUpdates(3, () =>
                {
                    if (__instance.Slot.ActiveUserRoot.ActiveUser != __instance.LocalUser) return;

                    Slot Assets = __instance.Slot.AddSlot("Laser Assets");

                    bool side = ____handler.Target.Side.Value == Chirality.Right;
                    string sideAsLR = side ? "R" : "L";

                    #region Components

                    BentTubeMesh NewMesh = Assets.AttachComponent<BentTubeMesh>();
                    NewMesh.Radius.Value = 0.002f;
                    NewMesh.Sides.Value = 6;
                    NewMesh.Segments.Value = 16;

                    BentTubeMesh OriginalMesh = __instance.Slot.GetComponent<BentTubeMesh>();

                    AssetRef<Mesh> Renderer = __instance.Slot.GetComponent<MeshRenderer>().Mesh;

                    // Original colors
                    ValueMultiDriver<colorX> baseStartColorDrives = Assets.AttachComponent<ValueMultiDriver<colorX>>();
                    ValueMultiDriver<colorX> baseEndColorDrives = Assets.AttachComponent<ValueMultiDriver<colorX>>();

                    DynamicValueVariable<colorX> originalStartColor = Assets.AttachComponent<DynamicValueVariable<colorX>>(); // Start
                    DynamicValueVariable<colorX> originalEndColor = Assets.AttachComponent<DynamicValueVariable<colorX>>(); // End
                    originalStartColor.VariableName.Value = string.Format(BaseColorVariable, sideAsLR, "Start");
                    originalEndColor.VariableName.Value = string.Format(BaseColorVariable, sideAsLR, "End");

                    // New colors
                    DynamicValueVariableDriver<colorX> newStartColor = Assets.AttachComponent<DynamicValueVariableDriver<colorX>>();
                    DynamicValueVariableDriver<colorX> newEndColor = Assets.AttachComponent<DynamicValueVariableDriver<colorX>>();
                    newStartColor.VariableName.Value = string.Format(NewColorVariable, sideAsLR, "Start");
                    newEndColor.VariableName.Value = string.Format(NewColorVariable, sideAsLR, "End");
                    newStartColor.DefaultValue.Value = StartColor;
                    newEndColor.DefaultValue.Value = EndColor;

                    // Smooth values
                    SmoothValue<colorX> newStartSmoothed = Assets.AttachComponent<SmoothValue<colorX>>();
                    SmoothValue<colorX> newEndSmoothed = Assets.AttachComponent<SmoothValue<colorX>>();
                    newStartSmoothed.Speed.Value = SmoothSpeed;
                    newEndSmoothed.Speed.Value = SmoothSpeed;

                    // End points
                    ValueMultiDriver<float3> directTargetPoints = Assets.AttachComponent<ValueMultiDriver<float3>>();
                    ValueMultiDriver<float3> actualTargetPoints = Assets.AttachComponent<ValueMultiDriver<float3>>();

                    #endregion

                    #region Drives

                    newStartColor.Target.Value = newStartSmoothed.TargetValue.ReferenceID;
                    newEndColor.Target.Value = newEndSmoothed.TargetValue.ReferenceID;

                    newStartSmoothed.Value.Value = NewMesh.StartPointColor.ReferenceID;
                    newEndSmoothed.Value.Value = NewMesh.EndPointColor.ReferenceID;

                    ____startColor.Value = baseStartColorDrives.Value.ReferenceID;
                    ____endColor.Value = baseEndColorDrives.Value.ReferenceID;

                    ____directPoint.ForceLink(directTargetPoints.Value);
                    ____actualPoint.ForceLink(actualTargetPoints.Value);

                    // Create all drives required
                    const int DRIVE_COUNT = 2;
                    for (int i = 0; i < DRIVE_COUNT; i++)
                    {
                        baseStartColorDrives.Drives.Add();
                        baseEndColorDrives.Drives.Add();
                        directTargetPoints.Drives.Add();
                        actualTargetPoints.Drives.Add();
                    }

                    baseStartColorDrives.Drives[0].ForceLink(OriginalMesh.StartPointColor);
                    baseStartColorDrives.Drives[1].ForceLink(originalStartColor.Value);

                    baseEndColorDrives.Drives[0].ForceLink(OriginalMesh.EndPointColor);
                    baseEndColorDrives.Drives[1].ForceLink(originalEndColor.Value);

                    directTargetPoints.Drives[0].ForceLink(OriginalMesh.DirectTargetPoint);
                    directTargetPoints.Drives[1].ForceLink(NewMesh.DirectTargetPoint);

                    actualTargetPoints.Drives[0].ForceLink(OriginalMesh.ActualTargetPoint);
                    actualTargetPoints.Drives[1].ForceLink(NewMesh.ActualTargetPoint);

                    #endregion

                    #region Laser data struct

                    LaserData thisLaser = new()
                    {
                        laser = __instance,
                        laserMesh = Renderer,

                        originalMesh = OriginalMesh,
                        newMesh = NewMesh,

                        startSmooth = newStartSmoothed,
                        endSmooth = newEndSmoothed,

                        newStartColor = newStartColor.DefaultValue,
                        newEndColor = newEndColor.DefaultValue
                    };

                    currentLasers.Add(thisLaser);

                    #endregion

                    if (Enabled) Renderer.Target = NewMesh;

                    __instance.Enabled = true;
                });
            }
        }

        #region Data Stuff

        static T GetConfigValue<T>(ModConfigurationKey<T> key, T defaultValue)
        {
            if (config != null) return config.GetValue(key) ?? defaultValue;
            return defaultValue;
        }

        struct LaserData
        {
            public InteractionLaser laser;

            public AssetRef<Mesh> laserMesh;

            public IAssetProvider<Mesh> originalMesh;
            public IAssetProvider<Mesh> newMesh;

            public SmoothValue<colorX> startSmooth;
            public SmoothValue<colorX> endSmooth;

            public Sync<colorX> newStartColor;
            public Sync<colorX> newEndColor;
        }

        static List<LaserData> currentLasers = [];

        #endregion
    }
}
