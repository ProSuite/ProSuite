using System.Collections.Generic;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.GeoDb;
using ProSuite.DomainModel.Core.DataModel;

namespace ProSuite.DomainModel.AO.DataModel
{
	/// <summary>
	/// Optional interface for model contexts that encapsulates look-up and opening of
	/// relationship classes and query tables using the join as defined by the relationship
	/// class.
	/// </summary>
	public interface IQueryTableContext
	{
		/// <summary>
		/// If fully qualified fields are used and need to be translated between
		/// model element and schema name, this method returns the schema name. This is 
		/// relevant for m:n or attributed relationship classes.
		/// </summary>
		/// <param name="associationName"></param>
		/// <param name="model"></param>
		/// <returns></returns>
		string GetRelationshipClassName([NotNull] string associationName, [NotNull] DdxModel model);

		/// <summary>
		/// Whether this specific implementation can open query tables or not.
		/// </summary>
		/// <returns></returns>
		bool CanOpenQueryTables();

		/// <summary>
		/// Opens a query table based on a relationship class. The rules are the same as for
		/// <see cref="RelationshipClassUtils.GetQueryTable"/>.
		/// </summary>
		/// <param name="relationshipClassName"></param>
		/// <param name="model"></param>
		/// <param name="tables"></param>
		/// <param name="joinType"></param>
		/// <param name="whereClause"></param>
		/// <returns></returns>
		IReadOnlyTable OpenQueryTable([NotNull] string relationshipClassName,
		                              [NotNull] DdxModel model,
		                              [NotNull] IList<IReadOnlyTable> tables,
		                              JoinType joinType,
		                              [CanBeNull] string whereClause);
	}
}
