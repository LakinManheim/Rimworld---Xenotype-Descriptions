using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace LocalRimworld.XenotypeDescriptions
{
    internal static class TemplateEditing
    {
        internal static void OpenDescription(Dialog_CreateXenogerm dialog, string name)
        {
            var prompt = new DescriptionPrompt(delegate(string text) { DescriptionStore.Set(dialog, text); }, delegate { });
            var packs = (List<Genepack>)AccessTools.Field(typeof(Dialog_CreateXenogerm), "selectedGenepacks").GetValue(dialog);
            string preview = DescriptionResolver.ForGenes(packs.SelectMany(p => p.GeneSet.GenesListForReading));
            Find.WindowStack.Add(new Dialog_XenotypeDescription(name, prompt, DescriptionStore.Get(dialog), preview));
        }

        internal static AcceptanceReport SaveTemplate(string name, XenotypeIconDef icon, List<Genepack> genepacks, Dialog_CreateXenogerm dialog)
        {
            // Use vanilla validation, replacement-by-name and the normal saved-template database.
            AcceptanceReport result = CustomXenogermUtility.SaveXenogermTemplate(name, icon, genepacks);
            if (result.Accepted)
            {
                CustomXenogerm saved = Find.CustomXenogermDatabase.CustomXenogermsForReading.First(t => t.name == name);
                DescriptionStore.Copy(dialog, saved);
            }
            return result;
        }

        internal static void LoadTemplate(Dialog_CreateXenogerm dialog, CustomXenogerm template)
        {
            // Copying null is intentional: loading a legacy template clears the previous draft.
            DescriptionStore.Copy(template, dialog);
        }
    }

    [HarmonyPatch(typeof(GeneCreationDialogBase), "DoBottomButtons")]
    internal static class DescriptionButtonPatch
    {
        internal static readonly Vector2 ButtonSize = (Vector2)AccessTools.Field(typeof(GeneCreationDialogBase), "ButSize").GetValue(null);

        private static void Postfix(GeneCreationDialogBase __instance, Rect rect, string ___xenotypeName)
        {
            var dialog = __instance as Dialog_CreateXenogerm;
            if (dialog != null && Widgets.ButtonText(FooterLayout.DescriptionButton(rect, ButtonSize), "XGD_Edit".Translate()))
                TemplateEditing.OpenDescription(dialog, ___xenotypeName);
        }
    }

    [HarmonyPatch(typeof(Dialog_CreateXenogerm), "DoBottomButtons")]
    internal static class DurationLayoutPatch
    {
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var nativeRect = typeof(Rect).GetConstructor(new[] { typeof(float), typeof(float), typeof(float), typeof(float) });
            var replacement = AccessTools.Method(typeof(DurationLayoutPatch), "SetDurationRect");
            var code = new List<CodeInstruction>(instructions);
            int found = 0;
            foreach (CodeInstruction instruction in code)
            {
                if (instruction.opcode == OpCodes.Call && object.Equals(instruction.operand, nativeRect))
                {
                    instruction.operand = replacement;
                    found++;
                }
            }
            if (found != 1) throw new InvalidOperationException("Xenotype Descriptions: expected one duration Rect constructor; found " + found);
            return code;
        }

        private static void SetDurationRect(ref Rect result, float x, float y, float width, float height)
        {
            Vector2 button = DescriptionButtonPatch.ButtonSize;
            float footerWidth = (width + button.x + 10f) * 2f;
            result = FooterLayout.Duration(new Rect(x - footerWidth / 2f, y, footerWidth, height), button);
        }
    }

    [HarmonyPatch(typeof(Dialog_CreateXenogerm), "DrawSearchRect")]
    internal static class SaveTemplatePatch
    {
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var save = AccessTools.Method(typeof(CustomXenogermUtility), "SaveXenogermTemplate");
            var replacement = AccessTools.Method(typeof(TemplateEditing), "SaveTemplate");
            var code = new List<CodeInstruction>(instructions);
            if (code.Count(i => i.Calls(save)) != 1) throw new InvalidOperationException("Xenotype Descriptions: expected one SaveXenogermTemplate call.");
            foreach (CodeInstruction instruction in code)
            {
                if (instruction.Calls(save))
                {
                    var dialog = new CodeInstruction(OpCodes.Ldarg_0);
                    dialog.labels.AddRange(instruction.labels);
                    dialog.blocks.AddRange(instruction.blocks);
                    instruction.labels.Clear();
                    instruction.blocks.Clear();
                    yield return dialog;
                    instruction.operand = replacement;
                }
                yield return instruction;
            }
        }
    }

    [HarmonyPatch]
    internal static class LoadTemplatePatch
    {
        private static MethodBase TargetMethod()
        {
            // Select by owner and signature, rather than the compiler's numbered lambda suffix.
            return AccessTools.GetDeclaredMethods(typeof(Dialog_CreateXenogerm)).Single(m =>
                m.Name.StartsWith("<DrawSearchRect>", StringComparison.Ordinal) && m.ReturnType == typeof(void) &&
                m.GetParameters().Length == 1 && m.GetParameters()[0].ParameterType == typeof(CustomXenogerm));
        }

        private static void Postfix(Dialog_CreateXenogerm __instance, CustomXenogerm __0) { TemplateEditing.LoadTemplate(__instance, __0); }
    }

    [HarmonyPatch(typeof(CustomXenogerm), "ExposeData")]
    internal static class TemplateSavePatch
    {
        private static void Postfix(CustomXenogerm __instance) { DescriptionPersistence.Expose(__instance); }
    }
}
