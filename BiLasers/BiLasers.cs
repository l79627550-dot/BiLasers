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

namespace RainbowLasers
{
    public class Patch : ResoniteMod
    {
        public override string Name => "BiLasers";
        public override string Author => "Nexis";
        public override string Link => "https://github.com/l79627550-dot/BiLasers";
        public override string Version => "1.0.0";

        public static ModConfiguration config;

        [AutoRegisterConfigKey]
        private static readonly ModConfigurationKey<bool> ENABLED = new ModConfigurationKey<bool>("enabled", "Enabled", () => true);

        [AutoRegisterConfigKey]
        private static readonly ModConfigurationKey<float> SMOOTH = new ModConfigurationKey<float>("Smoothing Value", "How fast do you want the laser to change color?", () => 10f);

        [AutoRegisterConfigKey]
        private static readonly ModConfigurationKey<colorX> START = new ModConfigurationKey<colorX>("Start Color", "Start Color:", () => new colorX(0.84f, 0.01f, 0.44f, 1f));

        [AutoRegisterConfigKey]
        private static readonly ModConfigurationKey<colorX> END = new ModConfigurationKey<colorX>("End Color", "End Color", () => new colorX(0f, 0.22f, 0.66f, 1f));

        public override void OnEngineInit()
        {
            config = GetConfiguration();
            config.Save(true);
            Harmony harmony = new Harmony("com.zahndy.RainbowLasers");
            harmony.PatchAll();
        }

        [HarmonyPatch(typeof(InteractionLaser))]
        class InteractionLaserPatch
        {
            [HarmonyPrefix]
            [HarmonyPatch("OnAwake")]
            static void Prefix(InteractionLaser __instance,
                               FieldDrive<colorX> ____startColor,
                               FieldDrive<colorX> ____endColor,
                               FieldDrive<float3> ____directPoint,
                               FieldDrive<float3> ____actualPoint,
                               SyncRef<InteractionHandler> ____handler)
            {
                __instance.RunInUpdates(3, () =>
                {
                    if (!config.GetValue(ENABLED)) return;
                    if (__instance.Slot.ActiveUserRoot.ActiveUser != __instance.LocalUser) return;

                    Slot Assets = __instance.Slot.AddSlot("Assets");

                    DynamicValueVariable<colorX> ColS = Assets.AttachComponent<DynamicValueVariable<colorX>>(); // Start
                    DynamicValueVariable<colorX> ColE = Assets.AttachComponent<DynamicValueVariable<colorX>>(); // End

                    DynamicValueVariableDriver<colorX> StartColor = Assets.AttachComponent<DynamicValueVariableDriver<colorX>>();
                    DynamicValueVariableDriver<colorX> EndColor = Assets.AttachComponent<DynamicValueVariableDriver<colorX>>();

                    SmoothValue<colorX> ColSS = Assets.AttachComponent<SmoothValue<colorX>>();
                    SmoothValue<colorX> ColES = Assets.AttachComponent<SmoothValue<colorX>>();

                    bool side = ____handler.Target.Side.Value == Chirality.Right;
                    StartColor.VariableName.Value = $"User/Laser_{(side ? "R" : "L")}_Start";
                    EndColor.VariableName.Value = $"User/Laser_{(side ? "R" : "L")}_End";
                    ColS.VariableName.Value = $"User/InteractionLaser_{(side ? "R" : "L")}_Start";
                    ColE.VariableName.Value = $"User/InteractionLaser_{(side ? "R" : "L")}_End";

                    BentTubeMesh Mesh = Assets.AttachComponent<BentTubeMesh>();
                    AssetRef<Mesh> Renderer = __instance.Slot.GetComponent<MeshRenderer>().Mesh;

                    StartColor.Target.Value = ColSS.TargetValue.ReferenceID;
                    EndColor.Target.Value = ColES.TargetValue.ReferenceID;

                    ColSS.Value.Value = Mesh.StartPointColor.ReferenceID;
                    ColES.Value.Value = Mesh.EndPointColor.ReferenceID;

                    ColSS.Speed.Value = config.GetValue(SMOOTH);
                    ColES.Speed.Value = config.GetValue(SMOOTH);

                    StartColor.DefaultValue.Value = config.GetValue(START);
                    EndColor.DefaultValue.Value = config.GetValue(END);

                    Renderer.Target = Mesh;
                    Mesh.Radius.Value = 0.002f;
                    Mesh.Sides.Value = 6;
                    Mesh.Segments.Value = 16;

                    ____startColor.Value = ColS.Value.ReferenceID;
                    ____endColor.Value = ColE.Value.ReferenceID;

                    ____directPoint.ForceLink(Mesh.DirectTargetPoint);
                    ____actualPoint.ForceLink(Mesh.ActualTargetPoint);

                    __instance.Enabled = true;
                });
            }
        }
    }
}
