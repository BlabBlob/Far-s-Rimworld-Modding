using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace TheFarawayDev.MatterAnywhere
{
    public enum NetworkMode
    {
        BiDirectional,
        PushOnly,
        PullOnly,
        Isolated
    }

    public class CompNetworkRelay : ThingComp
    {
        public NetworkMode currentMode = NetworkMode.BiDirectional;
        public int bandwidthLimit = int.MaxValue;
        private CompPowerTrader powerComp;

        public bool PowerOn => powerComp?.PowerOn ?? false;
        public CompPowerTrader PowerComp => powerComp;

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            powerComp = parent.GetComp<CompPowerTrader>();
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref currentMode, "currentMode", NetworkMode.BiDirectional);
            Scribe_Values.Look(ref bandwidthLimit, "bandwidthLimit", int.MaxValue);
            Scribe_Collections.Look(ref transferLogs, "transferLogs", LookMode.Value);
            if (transferLogs == null)
            {
                transferLogs = new List<string>();
            }
        }

        public override string CompInspectStringExtra()
        {
            string modeStr = "Quantum Relay Mode: " + currentMode.ToString();
            string limitDisplay = bandwidthLimit == int.MaxValue ? "Max" : bandwidthLimit.ToString();
            int maxLimit = GetMaxBandwidthLimit();
            string limitStr = $"Bandwidth: {limitDisplay} / {(maxLimit >= 99999 ? "Max" : maxLimit.ToString())}";
            string powerStr = "Power Draw: " + (powerComp != null ? (-powerComp.PowerOutput).ToString("F0") : "0") + " W";
            return modeStr + "\n" + limitStr + "\n" + powerStr;
        }

        public int GetMaxBandwidthLimit()
        {
            var projInstant = DefDatabase<ResearchProjectDef>.GetNamed("QuantumBandwidth_Instant", false);
            var projBandwidth3 = DefDatabase<ResearchProjectDef>.GetNamed("QuantumBandwidth_III", false);
            var projBandwidth2 = DefDatabase<ResearchProjectDef>.GetNamed("QuantumBandwidth_II", false);
            var projBandwidth1 = DefDatabase<ResearchProjectDef>.GetNamed("QuantumBandwidth_I", false);
            var projPlanetary = DefDatabase<ResearchProjectDef>.GetNamed("PlanetaryNetworking", false);

            if (DebugSettings.godMode) return 99999;
            if (projInstant?.IsFinished ?? false) return 99999;
            if (projBandwidth3?.IsFinished ?? false) return 10000;
            if (projBandwidth2?.IsFinished ?? false) return 2500;
            if (projBandwidth1?.IsFinished ?? false) return 500;
            if (projPlanetary?.IsFinished ?? false) return 100;
            return 0;
        }

        public int GetTickInterval()
        {
            var projInstant = DefDatabase<ResearchProjectDef>.GetNamed("QuantumBandwidth_Instant", false);
            var projBandwidth3 = DefDatabase<ResearchProjectDef>.GetNamed("QuantumBandwidth_III", false);
            var projBandwidth2 = DefDatabase<ResearchProjectDef>.GetNamed("QuantumBandwidth_II", false);

            if (DebugSettings.godMode) return 10;
            if (projInstant?.IsFinished ?? false) return 10;
            if (projBandwidth3?.IsFinished ?? false) return 60;
            if (projBandwidth2?.IsFinished ?? false) return 150;
            return 250;
        }

        public List<string> transferLogs = new List<string>();

        public void RecordTransfer(string message)
        {
            if (Find.TickManager == null) return;
            int ticks = Find.TickManager.TicksGame;
            string timeStr = ((ticks / 2500) % 24).ToString("D2") + ":" + ((ticks % 2500) / 41).ToString("D2");
            transferLogs.Insert(0, $"[{timeStr}] {message}");
            if (transferLogs.Count > 15)
            {
                transferLogs.RemoveAt(transferLogs.Count - 1);
            }
        }

        public float GetPredictedPowerDraw()
        {
            if (!PowerOn) return 0f;
            int limit = Math.Min(bandwidthLimit, GetMaxBandwidthLimit());
            if (limit >= 99999)
            {
                return 5000f; // max power
            }
            float power = 500f + (limit * 0.5f * 10f); // predict using average 0.5kg per item
            return Mathf.Min(power, 5000f);
        }

        public override void CompTick()
        {
            base.CompTick();
            if (parent.IsHashIntervalTick(GetTickInterval()))
            {
                ProcessTransfers();
            }
        }

        public void ProcessTransfers()
        {
            if (!PowerOn)
            {
                if (powerComp != null) powerComp.PowerOutput = -500f;
                return;
            }

            if (currentMode == NetworkMode.PushOnly || currentMode == NetworkMode.BiDirectional)
            {
                var networkBuilding = parent as SK_Matter_Network.NetworkBuilding;
                var network = networkBuilding?.ParentNetwork;
                var controller = network?.ActiveController;
                var container = controller?.innerContainer;

                if (Prefs.DevMode)
                {
                    Log.Message($"[MatterAnywhere] Relay ProcessTransfers on map {parent.Map?.uniqueID ?? -1}: Mode={currentMode}, Controller={(controller != null ? controller.ThingID : "null")}, ContainerCount={(container != null ? container.Count.ToString() : "null")}");
                }

                if (container != null && container.Count > 0)
                {
                    int maxLimit = GetMaxBandwidthLimit();
                    int transferLimit = Math.Min(bandwidthLimit, maxLimit);

                    if (Prefs.DevMode)
                    {
                        Log.Message($"[MatterAnywhere] - Bandwidth limits: UserLimit={bandwidthLimit}, TechLimit={maxLimit} => EffectiveLimit={transferLimit}");
                    }

                    if (transferLimit > 0)
                    {
                        float totalMass = 0f;
                        int itemsTransferred = 0;
                        var list = new List<Thing>(container);

                        foreach (var thing in list)
                        {
                            if (itemsTransferred >= transferLimit) break;
                            if (HarmonyPatches.virtualPlanetaryThings.Contains(thing)) continue;
                            if (thing.def.stackLimit <= 1) continue; // Only transfer stackable resources

                            int toTransfer = Math.Min(thing.stackCount, transferLimit - itemsTransferred);
                            if (toTransfer <= 0) continue;

                            string defName = thing.def.defName;
                            var tracker = MatterNetworkWorldTracker.Instance;
                            if (tracker != null)
                            {
                                if (tracker.globalResourceLedger.ContainsKey(defName))
                                    tracker.globalResourceLedger[defName] += toTransfer;
                                else
                                    tracker.globalResourceLedger[defName] = toTransfer;
                                HarmonyPatches.globalLedgerDirty = true;
                            }

                            float mass = thing.def.BaseMass;
                            if (mass <= 0f) mass = 0.1f;
                            totalMass += mass * toTransfer;
                            itemsTransferred += toTransfer;

                            RecordTransfer($"Pushed {toTransfer}x {thing.LabelNoCount}");

                            if (toTransfer == thing.stackCount)
                            {
                                container.Remove(thing);
                                thing.Destroy(DestroyMode.Vanish);
                            }
                            else
                            {
                                thing.stackCount -= toTransfer;
                            }
                        }

                        if (itemsTransferred > 0)
                        {
                            float power = 500f + (totalMass * 10f); // 10W per kg
                            power = Mathf.Min(power, 5000f);
                            if (powerComp != null) powerComp.PowerOutput = -power;
                        }
                        else
                        {
                            if (powerComp != null) powerComp.PowerOutput = -500f;
                        }
                    }
                    else
                    {
                        if (powerComp != null) powerComp.PowerOutput = -500f;
                    }
                }
                else
                {
                    if (powerComp != null) powerComp.PowerOutput = -500f;
                }
            }
            else
            {
                if (powerComp != null) powerComp.PowerOutput = -500f;
            }
        }
    }
}
