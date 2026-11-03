using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using Verse;

namespace LocalRimworld.XenotypeDescriptions
{
    [StaticConstructorOnStartup]
    internal static class Bootstrap
    {
        static Bootstrap()
        {
            var harmony = new Harmony("local.rimworldfixes.xenotypedescriptions");
            harmony.PatchAll();
            RimHudCompatibility.Apply(harmony);
            Log.Message("[Xenotype Descriptions] Initialized 0.3.0.");
        }
    }

    internal static class DescriptionPersistence
    {
        internal static void Expose(object owner)
        {
            string text = DescriptionStore.Get(owner);
            Scribe_Values.Look(ref text, "xgdDescription", null);
            if (Scribe.mode == LoadSaveMode.LoadingVars) DescriptionStore.Set(owner, text);
        }
    }

    [HarmonyPatch(typeof(Dialog_CreateXenogerm), "StartAssembly")]
    internal static class AssemblyDescriptionPatch
    {
        private static void Postfix(Dialog_CreateXenogerm __instance, Building_GeneAssembler ___geneAssembler)
        {
            if (___geneAssembler.Working) DescriptionStore.Copy(__instance, ___geneAssembler);
        }
    }

    [HarmonyPatch(typeof(Building_GeneAssembler), "ExposeData")]
    internal static class AssemblerSavePatch
    {
        private static void Postfix(Building_GeneAssembler __instance) { DescriptionPersistence.Expose(__instance); }
    }

    [HarmonyPatch(typeof(Building_GeneAssembler), "Reset")]
    internal static class AssemblerResetPatch
    {
        private static void Postfix(Building_GeneAssembler __instance) { DescriptionStore.Set(__instance, null); }
    }

    [HarmonyPatch(typeof(Building_GeneAssembler), "Finish")]
    internal static class AssemblerFinishPatch
    {
        private static readonly MethodInfo Initialize = AccessTools.Method(typeof(Xenogerm), "Initialize");
        private static readonly MethodInfo InitializeWithDescription = AccessTools.Method(typeof(AssemblerFinishPatch), "InitializeProduct");

        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var code = new List<CodeInstruction>(instructions);
            int found = 0;
            foreach (CodeInstruction instruction in code) if (instruction.Calls(Initialize)) found++;
            if (found != 1) throw new InvalidOperationException("Xenotype Descriptions: expected one Xenogerm.Initialize call in GeneAssembler.Finish; found " + found);
            foreach (CodeInstruction instruction in code)
            {
                if (instruction.Calls(Initialize))
                {
                    var owner = new CodeInstruction(OpCodes.Ldarg_0);
                    // Preserve any branch targets / exception boundaries at the replaced call.
                    owner.labels.AddRange(instruction.labels);
                    owner.blocks.AddRange(instruction.blocks);
                    instruction.labels.Clear();
                    instruction.blocks.Clear();
                    yield return owner;
                    instruction.opcode = OpCodes.Call;
                    instruction.operand = InitializeWithDescription;
                }
                yield return instruction;
            }
        }

        private static void InitializeProduct(Xenogerm germ, List<Genepack> genepacks, string name, XenotypeIconDef icon, Building_GeneAssembler assembler)
        {
            germ.Initialize(genepacks, name, icon);
            DescriptionStore.Copy(assembler, germ);
        }
    }

    [HarmonyPatch(typeof(Xenogerm), "ExposeData")]
    internal static class XenogermSavePatch
    {
        private static void Postfix(Xenogerm __instance) { DescriptionPersistence.Expose(__instance); }
    }

    [HarmonyPatch(typeof(GeneUtility), "ImplantXenogermItem")]
    internal static class ImplantPatch
    {
        private static void Postfix(Pawn pawn, Xenogerm xenogerm)
        {
            if (ModsConfig.BiotechActive && pawn.genes != null && xenogerm.GeneSet != null)
                DescriptionStore.Copy(xenogerm, pawn.genes);
        }
    }

    [HarmonyPatch(typeof(GeneUtility), "ReimplantXenogerm")]
    internal static class ReimplantPatch
    {
        private static void Prefix(Pawn caster, out string __state)
        {
            // Vanilla can clear an exhausted donor's xenotype during extraction.
            __state = DescriptionStore.Get(caster.genes);
        }

        private static void Postfix(Pawn recipient, string __state)
        {
            if (ModsConfig.BiotechActive && recipient.genes != null)
                DescriptionStore.Set(recipient.genes, __state);
        }
    }

    [HarmonyPatch(typeof(Pawn_GeneTracker), "ExposeData")]
    internal static class PawnSavePatch
    {
        private static void Postfix(Pawn_GeneTracker __instance) { DescriptionPersistence.Expose(__instance); }
    }

    [HarmonyPatch]
    internal static class PawnResetPatch
    {
        private static IEnumerable<MethodBase> TargetMethods()
        {
            yield return AccessTools.Method(typeof(Pawn_GeneTracker), "SetXenotype");
            yield return AccessTools.Method(typeof(Pawn_GeneTracker), "SetXenotypeDirect");
            yield return AccessTools.Method(typeof(Pawn_GeneTracker), "ClearXenogenes");
        }

        private static void Postfix(Pawn_GeneTracker __instance) { DescriptionStore.Set(__instance, null); }
    }

    [HarmonyPatch(typeof(Pawn_GeneTracker), "XenotypeDescShort", MethodType.Getter)]
    internal static class PawnDescriptionPatch
    {
        private static void Postfix(Pawn_GeneTracker __instance, ref string __result)
        {
            string text = DescriptionResolver.GetOverride(__instance);
            if (text != null) __result = text;
        }
    }
}
