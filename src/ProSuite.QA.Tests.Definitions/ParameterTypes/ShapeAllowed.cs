using System;

namespace ProSuite.QA.Tests.ParameterTypes
{
	/// <summary>
	/// allowed topology types
	/// </summary>
	[Flags]
	public enum ShapeAllowed
	{
		/// <summary>
		/// Reported shape errors:
		/// -Branches
		/// -Cycles
		/// -InsideBranches
		/// </summary>
		None = 0,

		/// <summary>
		/// Circular connected lines are allowed
		/// 
		/// Reported shape errors:
		/// -InsideBranches
		/// </summary>
		Cycles = 1,

		/// <summary>
		/// Branching lines are allowed, that means common end points of 3 or more lines
		/// 
		/// Reported shape errors:
		/// -Cycles
		/// -InsideBranches
		/// </summary>
		Branches = 2,

		/// <summary>
		/// A branch toward the inside of a circular area may exist
		/// 
		/// Reported shape errors:
		/// -Cycles
		/// </summary>
		InsideBranches = 4,

		/// <summary>
		/// Cycles and branches are allowed
		/// 
		/// Reported shape errors:
		/// -InsideBranches
		/// </summary>
		CyclesAndBranches = 3,

		/// <summary>
		/// Cycles and branches and inside branches are allowed
		/// 
		/// Reported shape errors:
		/// ---
		/// </summary>
		All = 7
	}
}
