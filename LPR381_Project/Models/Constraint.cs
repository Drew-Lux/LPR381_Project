using System;
namespace LPR381_Project.Models
{
	public class Constraint
	{
		public double[] Coefficients { get; set; } = new double[0];

		public string Operator { get; set; } = string.Empty;

		public double RightHandSide { get; set; }
	}
}

