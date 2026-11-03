using System.Collections.Generic;
using RimWorld;
using Verse;

namespace LocalRimworld.XenotypeDescriptions
{
    internal static class DescriptionResolver
    {
        internal static string GetOverride(Pawn_GeneTracker genes)
        {
            string manual = DescriptionStore.Get(genes);
            if (manual != null || genes == null) return manual;
            if (!genes.UniqueXenotype) return null;

            // The implanted set is already unique. Include suppressed genes: they
            // are still part of the xenogerm, unlike the pawn's inherited genes.
            List<Gene> xenogenes = genes.Xenogenes;
            for (int i = 0; i < xenogenes.Count; i++)
                if (xenogenes[i].def.biostatArc > 0) return ForCount(0, true);
            return ForCount(xenogenes.Count, false);
        }

        internal static string ForGenes(IEnumerable<GeneDef> genes)
        {
            // Different selected packs may contain the same gene. Count the
            // resulting set once, matching the assembled xenogerm.
            var unique = new HashSet<GeneDef>();
            foreach (GeneDef gene in genes)
            {
                if (gene.biostatArc > 0) return ForCount(0, true);
                unique.Add(gene);
            }
            return ForCount(unique.Count, false);
        }

        private static string ForCount(int ordinaryGenes, bool archite)
        {
            if (archite) return "XGD_AutoArchite".Translate();
            if (ordinaryGenes == 0) return null;
            if (ordinaryGenes <= 5) return "XGD_AutoLight".Translate();
            if (ordinaryGenes <= 10) return "XGD_AutoModified".Translate();
            return "XGD_AutoDeep".Translate();
        }
    }
}
