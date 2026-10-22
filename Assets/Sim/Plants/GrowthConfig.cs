namespace PlantNursery.Sim.Plants
{
    /// <summary>Care grade from the care score (docs/design/plants.md#quality).</summary>
    public enum CareGrade { A, B, C }

    /// <summary>Tunable growth, health and quality values. Defaults match docs/design/plants.md.</summary>
    public sealed class GrowthConfig
    {
        /// <summary>Hours a plant may be root-bound before it starts losing health.</summary>
        public float RootBoundGraceHours = 24f;
        /// <summary>Health lost per hour once root-bound past the grace period.</summary>
        public float RootBoundHealthLossPerHour = 2f;
        /// <summary>Health lost per hour while wilting (moisture 0).</summary>
        public float WiltHealthLossPerHour = 2f;
        /// <summary>Health regained per hour while well watered and not root-bound past grace.</summary>
        public float RecoverPerHour = 1f;
        /// <summary>Moisture at or above which the plant is well watered.</summary>
        public float WellWateredMoisture = 30f;
        /// <summary>Minimum care score for grade A.</summary>
        public float GradeAMin = 0.9f;
        /// <summary>Minimum care score for grade B.</summary>
        public float GradeBMin = 0.7f;

        /// <summary>Grade for a care score.</summary>
        public CareGrade GradeFor(float careScore)
        {
            if (careScore >= GradeAMin) return CareGrade.A;
            return careScore >= GradeBMin ? CareGrade.B : CareGrade.C;
        }
    }
}
