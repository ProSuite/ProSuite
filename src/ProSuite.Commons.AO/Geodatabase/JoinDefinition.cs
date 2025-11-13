using System;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.GeoDb;

namespace ProSuite.Commons.AO.Geodatabase
{
	internal abstract class JoinDefinition
	{
		private readonly JoinType _joinType;

		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="JoinDefinition"/> class.
		/// </summary>
		/// <param name="joinType">Type of the join.</param>
		protected JoinDefinition(JoinType joinType)
		{
			_joinType = joinType;
		}

		#endregion

		[NotNull]
		public abstract string GetTableList();

		[NotNull]
		public string GetJoinCondition()
		{
			return GetJoinCondition(_joinType);
		}

		[NotNull]
		public string GetTableJoinStatement(bool ignoreFirstTable)
		{
			return GetTableJoinStatement(_joinType, ignoreFirstTable);
		}

		[NotNull]
		protected abstract string GetJoinCondition(JoinType joinType);

		[NotNull]
		protected abstract string GetTableJoinStatement(JoinType joinType,
		                                                bool ignoreFirstTable);

		[NotNull]
		protected static JoinExpressionWriter CreateJoinExpressionWriter(
			[NotNull] IWorkspace workspace)
		{
			var connectionInfo = workspace as IDatabaseConnectionInfo2;

			if (connectionInfo == null)
			{
				return new UnknownJoinExpressionWriter();
			}

			switch (connectionInfo.ConnectionDBMS)
			{
				case esriConnectionDBMS.esriDBMS_Oracle:
					return new OracleJoinExpressionWriter();

				case esriConnectionDBMS.esriDBMS_SQLServer:
					return new SqlServerJoinExpressionWriter();

				case esriConnectionDBMS.esriDBMS_Unknown:
				case esriConnectionDBMS.esriDBMS_Informix:
				case esriConnectionDBMS.esriDBMS_DB2:
				case esriConnectionDBMS.esriDBMS_PostgreSQL:
					return new UnknownJoinExpressionWriter();

				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		[NotNull]
		protected static string GetInnerJoinExpression([NotNull] string leftField,
		                                               [NotNull] string rightField)
		{
			return $"{leftField} = {rightField}";
		}

		#region Nested types

		protected abstract class JoinExpressionWriter
		{
			[NotNull]
			public abstract string GetExpression([NotNull] string leftField,
			                                     [NotNull] string rightField,
			                                     JoinType joinType);
		}

		private class UnknownJoinExpressionWriter : JoinExpressionWriter
		{
			public override string GetExpression(string leftField,
			                                     string rightField,
			                                     JoinType joinType)
			{
				switch (joinType)
				{
					case JoinType.InnerJoin:
						return GetInnerJoinExpression(leftField, rightField);

					case JoinType.LeftJoin:
					case JoinType.RightJoin:
						throw new ArgumentException(
							"Cannot generate join expression for type: " + joinType);

					default:
						throw new ArgumentException("Unhandled join type: " + joinType);
				}
			}
		}

		private class SqlServerJoinExpressionWriter : JoinExpressionWriter
		{
			public override string GetExpression(string leftField,
			                                     string rightField,
			                                     JoinType joinType)
			{
				switch (joinType)
				{
					case JoinType.InnerJoin:
						return GetInnerJoinExpression(leftField, rightField);

					case JoinType.LeftJoin:
						return $"{leftField} *= {rightField}";

					case JoinType.RightJoin:
						return $"{leftField} =* {rightField}";

					default:
						throw new ArgumentException("Unhandled join type: " + joinType);
				}
			}
		}

		private class OracleJoinExpressionWriter : JoinExpressionWriter
		{
			public override string GetExpression(string leftField, string rightField,
			                                     JoinType joinType)
			{
				switch (joinType)
				{
					case JoinType.InnerJoin:
						return GetInnerJoinExpression(leftField, rightField);

					case JoinType.LeftJoin:
						return $"{leftField} = {rightField} (+)";

					case JoinType.RightJoin:
						return $"{leftField} (+) = {rightField}";

					default:
						throw new ArgumentException("Unhandled join type: " + joinType);
				}
			}
		}

		#endregion
	}
}
