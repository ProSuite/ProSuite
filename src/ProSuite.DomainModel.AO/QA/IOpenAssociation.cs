using System.Collections.Generic;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.Core.DataModel;

namespace ProSuite.DomainModel.AO.QA
{
	/// Optional interface for dataset openers that encapsulates look-up and opening of
	/// relationship classes and database query tables using the join as defined by the relationship
	/// class.
	/// In the long term a different implementation than the database join via relationship class
	/// will be possible while maintaining the interface.
	public interface IOpenAssociation
	{
		/// <summary>
		/// Opens a query table based on an association. The rules are the same as for
		/// <see cref="RelationshipClassUtils.GetQueryTable"/>.
		/// </summary>
		/// <param name="association"></param>
		/// <param name="tables"></param>
		/// <param name="joinType"></param>
		/// <param name="whereClause"></param>
		/// <returns></returns>
		IReadOnlyTable OpenQueryTable([NotNull] Association association,
		                              [NotNull] IList<IReadOnlyTable> tables,
		                              JoinType joinType,
		                              [CanBeNull] string whereClause = null);

		/// <summary>
		/// Opens a query table based on an association name. The rules are the same as for
		/// <see cref="RelationshipClassUtils.GetQueryTable"/>.
		/// </summary>
		/// <param name="associationName"></param>
		/// <param name="model"></param>
		/// <param name="tables"></param>
		/// <param name="joinType"></param>
		/// <param name="whereClause"></param>
		/// <returns></returns>
		IReadOnlyTable OpenQueryTable([NotNull] string associationName,
		                              [NotNull] DdxModel model,
		                              [NotNull] IList<IReadOnlyTable> tables,
		                              JoinType joinType,
		                              [CanBeNull] string whereClause = null);

		/// <summary>
		/// Returns the (fully qualified) name of the relationship class referenced by
		/// an association.
		/// If fully qualified fields are used and need to be translated between
		/// model element and schema name, this method returns the schema name. This is 
		/// relevant for m:n or attributed relationship classes.
		/// </summary>
		/// <param name="associationName"></param>
		/// <param name="model"></param>
		/// <returns></returns>
		string GetRelationshipClassName([NotNull] string associationName,
		                                [NotNull] DdxModel model);
	}
}
