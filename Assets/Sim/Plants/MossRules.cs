using System;
using System.Linq;
using PlantNursery.Sim.Content;

namespace PlantNursery.Sim.Plants
{
    /// <summary>Pure rules for harvest-and-regrow plants (Hearthmoss) and plant quality.</summary>
    public static class MossRules
    {
        /// <summary>Gene that scales regrow time.</summary>
        public const string RegrowthGene = "regrowthSpeed";

        /// <summary>
        /// True when the plant can be scraped now: a living species plant in its final stage whose species regrows,
        /// not already regrowing.
        /// </summary>
        public static bool IsHarvestable(Plant p, ContentDb content)
        {
            if (p == null || p.IsDead || !p.IsSpeciesPlant || p.IsRegrowing) return false;
            if (!content.SpeciesDefs.TryGetValue(p.SpeciesId, out var sp) || sp.RegrowDays <= 0) return false;
            return p.StageIndex >= sp.Stages.Count - 1;
        }

        /// <summary>Regrow time in ticks: regrowDays x (1.25 - 0.5 x regrowthSpeed phenotype) x ticks per day, at least 1.</summary>
        public static int RegrowTicks(Plant p, SpeciesDef sp, int ticksPerDay)
        {
            var speed = p.Genome != null && sp.GeneIds.Contains(RegrowthGene) ? p.Genome.BlendPhenotype(RegrowthGene) : 0f;
            var days = sp.RegrowDays * (1.25f - 0.5f * speed);
            return Math.Max(1, (int)Math.Round(days * ticksPerDay));
        }

        /// <summary>Quality: care grade (plants.md Quality) minus the steps lost, never below poor.</summary>
        public static QualityGrade QualityOf(Plant p, EconomyConfig economy)
        {
            var grade = (int)QualityGrade.Good; // no ticks alive counts as good
            if (p.TicksAlive > 0)
            {
                var share = (float)p.TicksNotDry / p.TicksAlive;
                grade = share >= economy.ExcellentCareThreshold ? (int)QualityGrade.Excellent
                    : share < economy.PoorCareThreshold ? (int)QualityGrade.Poor : (int)QualityGrade.Good;
            }
            return (QualityGrade)Math.Max((int)QualityGrade.Poor, grade - p.QualityStepsLost);
        }
    }
}
