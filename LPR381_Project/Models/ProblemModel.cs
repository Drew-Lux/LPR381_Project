using System;
using System.Collections.Generic;

namespace LPR381_Project.Models
{
	public class ProblemModel
	{
		public string ObjectiveType { get; set; } = "max";

		public double[] ObjectiveCoefficients { get; set; } = new double[0];

		public List<Constraint> Constraints { get; set; } = new List<Constraint>();

		public string[] SignRestrictions { get; set; } = new string[0];
	}
}

