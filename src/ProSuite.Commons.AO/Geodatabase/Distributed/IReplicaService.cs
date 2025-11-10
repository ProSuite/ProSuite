using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AO.Geodatabase.Distributed
{
	public interface IReplicaService
	{
		/// <summary>
		/// Checks out a replica based on the specified replica description and replica name.
		/// </summary>
		/// <param name="replicaDescription"></param>
		/// <param name="reuseSchema">Whether an existing schema in the check-out workspace should
		/// be re-used.</param>
		/// <param name="replicaName">The name to be used to name the replica version in the
		/// central workspace.</param>
		IWorkspace CheckOut([NotNull] IReplicaDescription replicaDescription,
		                    [NotNull] string replicaName,
		                    bool reuseSchema);
	}
}
