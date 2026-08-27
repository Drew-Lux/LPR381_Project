using System;

namespace LPR381_Project.Models
{
    /// <summary>
    /// Represents a single constraint in the original LP/IP model.
    ///
    /// Example:
    ///
    /// 2x1 + 3x2 <= 10
    ///
    /// is represented by:
    ///
    /// Coefficients = [2, 3]
    /// Operator = "<="
    /// RightHandSide = 10
    /// </summary>
    public class Constraint
    {
        /// <summary>
        /// Coefficients of the decision variables.
        /// </summary>
        public double[] Coefficients { get; set; }
            = Array.Empty<double>();

        /// <summary>
        /// Constraint relation:
        ///
        /// =
        /// <=
        /// >=
        /// </summary>
        public string Operator { get; set; } = "<=";

        /// <summary>
        /// Right-hand-side value.
        /// </summary>
        public double RightHandSide { get; set; }

        /// <summary>
        /// Creates a deep copy of this constraint.
        /// </summary>
        public Constraint Clone()
        {
            return new Constraint
            {
                Coefficients =
                    (double[])Coefficients.Clone(),

                Operator = Operator,

                RightHandSide = RightHandSide
            };
        }
    }
}