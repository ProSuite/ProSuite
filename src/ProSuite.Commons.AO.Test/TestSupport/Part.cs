using System.Collections.Generic;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AO.Test.TestSupport
{
	public class Part
	{
		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="Part"/> class.
		/// </summary>
		/// <param name="points">The points.</param>
		public Part(params Pt[] points) : this((IEnumerable<Pt>) points) { }

		/// <summary>
		/// Initializes a new instance of the <see cref="Part"/> class.
		/// </summary>
		/// <param name="points">The points.</param>
		public Part([NotNull] IEnumerable<Pt> points)
		{
			Points = new List<Pt>(points).AsReadOnly();
		}

		#endregion

		[NotNull]
		public IList<Pt> Points { get; }
	}
}
