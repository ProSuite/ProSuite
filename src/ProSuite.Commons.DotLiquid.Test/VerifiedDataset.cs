using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.DotLiquid.Test
{
	public class VerifiedDataset
	{
		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="VerifiedDataset"/> class.
		/// </summary>
		[UsedImplicitly]
		public VerifiedDataset() { }

		/// <summary>
		/// Initializes a new instance of the <see cref="VerifiedDataset"/> class.
		/// </summary>
		/// <param name="name">The name.</param>
		/// <param name="workspaceName">Name of the workspace.</param>
		public VerifiedDataset([NotNull] string name, [CanBeNull] string workspaceName)
		{
			Assert.ArgumentNotNullOrEmpty(name, "name");

			Name = name;
			WorkspaceName = workspaceName;
		}

		#endregion

		[UsedImplicitly]
		public string Name { get; set; }

		[UsedImplicitly]
		public string WorkspaceName { get; set; }
	}
}
