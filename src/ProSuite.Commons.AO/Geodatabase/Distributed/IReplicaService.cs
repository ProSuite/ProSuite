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
		/// <returns>The checked-out workspace.</returns>
		IWorkspace CheckOut([NotNull] IReplicaDescription replicaDescription,
		                    [NotNull] string replicaName,
		                    bool reuseSchema);

		/// <summary>
		/// Checks in the specified replica by applying the delta from the check-out workspace to
		/// the parent workspace.
		/// </summary>
		/// <param name="parentWorkspace"></param>
		/// <param name="replicaName"></param>
		/// <param name="checkoutWorkspace"></param>
		ICheckInDelta CheckIn([NotNull] IWorkspace parentWorkspace,
		                      [NotNull] string replicaName,
		                      [NotNull] IWorkspace checkoutWorkspace);

		bool ExistsCheckoutDatabase(string checkOutDatabaseName);

		/// <summary>
		/// Determines whether the parent replica in the central workspace exists.
		/// </summary>
		/// <param name="workspace"></param>
		/// <param name="replicaName"></param>
		/// <returns></returns>
		bool ExistsReplica(IWorkspace workspace, string replicaName);

		void UnregisterReplica(IWorkspace workspace, string replicaName);

		void DeleteCheckoutDatabase(string databaseName);
	}
}
