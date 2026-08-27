using System;
using System.Collections.Generic;

namespace LPR381_Project.Models
{
    /// <summary>
    /// Represents the original LP/IP model supplied by the user.
    ///
    /// This class represents the mathematical problem before
    /// canonical-form conversion.
    ///
    /// Algorithms should avoid modifying the original model directly.
    /// Use Clone() when creating a modified sub-problem.
    /// </summary>
    public class ProblemModel
    {
        /// <summary>
        /// Objective type: "max" or "min".
        /// </summary>
        public string ObjectiveType { get; set; } = "max";

        /// <summary>
        /// Objective-function coefficients.
        /// </summary>
        public double[] ObjectiveCoefficients { get; set; }
            = Array.Empty<double>();

        /// <summary>
        /// Original constraints.
        /// </summary>
        public List<Constraint> Constraints { get; set; }
            = new List<Constraint>();

        /// <summary>
        /// Sign restrictions for the original variables.
        ///
        /// Supported values:
        /// +, -, urs, int, bin
        /// </summary>
        public string[] SignRestrictions { get; set; }
            = Array.Empty<string>();

        /// <summary>
        /// Creates a deep copy of the model.
        ///
        /// This is particularly important for Branch & Bound,
        /// where each node requires its own modified problem.
        /// </summary>
        public ProblemModel Clone()
        {
            ProblemModel clone = new ProblemModel();

            clone.ObjectiveType = ObjectiveType;

            clone.ObjectiveCoefficients =
                (double[])ObjectiveCoefficients.Clone();

            clone.SignRestrictions =
                (string[])SignRestrictions.Clone();

            foreach (Constraint constraint in Constraints)
            {
                clone.Constraints.Add(constraint.Clone());
            }

            return clone;
        }

        /// <summary>
        /// Adds a constraint to the model.
        /// </summary>
        public void AddConstraint(Constraint constraint)
        {
            if (constraint == null)
            {
                throw new ArgumentNullException(
                    nameof(constraint));
            }

            Constraints.Add(constraint);
        }
    }
}