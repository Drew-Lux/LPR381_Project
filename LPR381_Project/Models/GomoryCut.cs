using System;

namespace LPR381_Project.Models
{
    /// <summary>
    /// Records one Gomory fractional cut generated during the
    /// Cutting Plane algorithm.
    ///
    /// The cut is derived from the row of the current optimal
    /// tableau that belongs to an integer-restricted basic variable
    /// whose value is fractional. It is stored purely for display /
    /// export purposes - the actual constraint that gets appended to
    /// the working canonical model lives inside
    /// <see cref="CuttingPlaneSolver"/>.
    /// </summary>
    public class GomoryCut
    {
        /// <summary>
        /// 1-based sequence number of this cut (Cut 1, Cut 2, ...).
        /// </summary>
        public int CutNumber { get; set; }

        /// <summary>
        /// Index (0-based) of the tableau row the cut was derived
        /// from.
        /// </summary>
        public int SourceRow { get; set; }

        /// <summary>
        /// Name of the fractional basic variable in the source row.
        /// </summary>
        public string SourceVariable { get; set; } = string.Empty;

        /// <summary>
        /// Value of the fractional basic variable before the cut was
        /// applied.
        /// </summary>
        public double SourceValue { get; set; }

        /// <summary>
        /// The source tableau row (B^-1 * A), one entry per canonical
        /// column that existed before this cut was added.
        /// </summary>
        public double[] TableauRow { get; set; } = Array.Empty<double>();

        /// <summary>
        /// Fractional parts of <see cref="TableauRow"/>. These become
        /// the coefficients of the new Gomory constraint (before the
        /// sign flip used to make it dual-simplex ready).
        /// </summary>
        public double[] FractionalCoefficients { get; set; } = Array.Empty<double>();

        /// <summary>
        /// Fractional part of the row's right-hand-side.
        /// </summary>
        public double RightHandSideFraction { get; set; }

        /// <summary>
        /// Name assigned to the new Gomory slack variable introduced
        /// by this cut (e.g. "g1").
        /// </summary>
        public string SlackVariableName { get; set; } = string.Empty;

        /// <summary>
        /// Human-readable inequality, in the classic
        /// "sum(f_j * x_j) >= f_i" form, for display/export.
        /// </summary>
        public string Description { get; set; } = string.Empty;
    }
}
