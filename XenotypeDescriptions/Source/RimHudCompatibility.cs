using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using Verse;

namespace LocalRimworld.XenotypeDescriptions
{
    // Optional compatibility: no assembly reference or hard dependency on RimHUD.
    internal static class RimHudCompatibility
    {
        internal static void Apply(Harmony harmony)
        {
            Type type = AccessTools.TypeByName("RimHUD.Interface.Screen.InspectPaneButtons");
            if (type == null) return;
            harmony.Patch(AccessTools.Method(type, "DrawXenotype"),
                transpiler: new HarmonyMethod(AccessTools.Method(typeof(RimHudCompatibility), "Transpiler")));
        }

        private static string GetDescription(Pawn_GeneTracker genes)
        {
            // Share automatic/manual text with vanilla. Native named xenotypes
            // retain RimHUD's full description; empty custom sets use vanilla's
            // unique-xenotype text instead of their underlying baseliner Def.
            return DescriptionResolver.GetOverride(genes) ??
                (genes.UniqueXenotype ? genes.XenotypeDescShort : genes.Xenotype.description);
        }

        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var getter = AccessTools.PropertyGetter(typeof(Pawn_GeneTracker), "Xenotype");
            var field = AccessTools.Field(typeof(Def), "description");
            var replacement = AccessTools.Method(typeof(RimHudCompatibility), "GetDescription");
            var code = new List<CodeInstruction>(instructions);
            int found = 0;
            for (int i = 0; i + 1 < code.Count; i++)
                if (code[i].Calls(getter) && code[i + 1].opcode == OpCodes.Ldfld && object.Equals(code[i + 1].operand, field)) found++;
            if (found != 2) throw new InvalidOperationException("Xenotype Descriptions: expected two RimHUD xenotype description reads; found " + found);
            for (int i = 0; i < code.Count; i++)
            {
                if (i + 1 < code.Count && code[i].Calls(getter) && code[i + 1].opcode == OpCodes.Ldfld && object.Equals(code[i + 1].operand, field))
                {
                    var read = new CodeInstruction(OpCodes.Call, replacement);
                    read.labels.AddRange(code[i].labels);
                    read.labels.AddRange(code[i + 1].labels);
                    read.blocks.AddRange(code[i].blocks);
                    read.blocks.AddRange(code[i + 1].blocks);
                    yield return read;
                    i++;
                }
                else yield return code[i];
            }
        }
    }
}
