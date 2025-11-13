using System;
using System.Collections.Generic;
using System.Text;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.GeoDb;
using ProSuite.Commons.Text;

namespace ProSuite.Commons.AO.Geodatabase
{
	internal class RelationshipClassJoinDefinition : JoinDefinition
	{
		[NotNull] private readonly IRelationshipClass _relClass;
		private JoinExpressionWriter _joinExpressionWriter;
		private readonly IList<string> _tableNames;
		private readonly IFeatureClass _baseFeatureClass;
		private readonly int _featureClassCount;

		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="RelationshipClassJoinDefinition"/> class.
		/// </summary>
		/// <param name="relClass">The relationship class.</param>
		/// <param name="joinType">The join type.</param>
		/// <param name="baseFeatureClass">The primary feature class containing the shape field
		/// in for joins between two feature classes. In the case of an outer join it must not be
		/// on the outer side, i.e. no null values must exist in the base feature class side.</param>
		internal RelationshipClassJoinDefinition(
			[NotNull] IRelationshipClass relClass,
			JoinType joinType,
			[CanBeNull] IFeatureClass baseFeatureClass = null)
			: base(joinType)
		{
			Assert.ArgumentNotNull(relClass, nameof(relClass));

			_relClass = relClass;

			if (baseFeatureClass == null)
			{
				_tableNames = GetTableNames(relClass, out _baseFeatureClass,
				                            out _featureClassCount);
			}
			else
			{
				_baseFeatureClass = baseFeatureClass;

				Assert.True(IsInvolvedFeatureClass(baseFeatureClass, relClass),
				            "The base feature class must participate in the relationship class");

				_tableNames = GetTableNames(relClass, baseFeatureClass, joinType,
				                            out _featureClassCount);
			}
		}

		#endregion

		[NotNull]
		public IEnumerable<string> TableNames => _tableNames;

		[CanBeNull]
		public IFeatureClass BaseFeatureClass => _baseFeatureClass;

		public int FeatureClassCount => _featureClassCount;

		public override string GetTableList()
		{
			return StringUtils.Concatenate(_tableNames, ",");
			//IRelationshipClass relationshipClass = _relClass;

			//string originClassName = ((IDataset)relationshipClass.OriginClass).Name;
			//string destClassName = ((IDataset)relationshipClass.DestinationClass).Name;
			//string relClassName = ((IDataset)relationshipClass).Name;

			//esriRelCardinality cardinality = relationshipClass.Cardinality;

			//if (RelationshipClassUtils.UsesRelationshipTable(relationshipClass))
			//{
			//    // BUG in 9.3 SP1:
			//    // Geometries get assigned to wrong features if the order in table list
			//    // is not table, bridge-table, featureclass

			//    return relationshipClass.OriginClass is IFeatureClass
			//            ? string.Format("{0},{1},{2}",
			//                            destClassName, relClassName, originClassName)
			//            : string.Format("{0},{1},{2}",
			//                            originClassName, relClassName, destClassName);
			//}

			//// handle non-attributed relationships (i.e. fk is on destination table)
			//switch (cardinality)
			//{
			//    case esriRelCardinality.esriRelCardinalityOneToOne:
			//        return string.Format("{0},{1}", originClassName, destClassName);

			//    case esriRelCardinality.esriRelCardinalityOneToMany:
			//        return string.Format("{0},{1}", originClassName, destClassName);

			//    default:
			//        throw CreateUnsupportedNonAttributedCardinalityException(cardinality);
			//}
		}

		[NotNull]
		private IList<string> GetTableNames(
			[NotNull] IRelationshipClass relationshipClass,
			[CanBeNull] out IFeatureClass baseFeatureClass,
			out int featureClassCount)
		{
			Assert.ArgumentNotNull(relationshipClass, nameof(relationshipClass));

			var result = new List<string>();

			IObjectClass originClass = relationshipClass.OriginClass;
			IObjectClass destinationClass = relationshipClass.DestinationClass;

			var originFeatureClass = originClass as IFeatureClass;
			var destinationFeatureClass = destinationClass as IFeatureClass;

			featureClassCount = GetInvolvedFeatureClassCount(relationshipClass);

			string originClassName = ((IDataset) originClass).Name;
			string destClassName = ((IDataset) destinationClass).Name;
			string relClassName = ((IDataset) relationshipClass).Name;

			esriRelCardinality cardinality = relationshipClass.Cardinality;

			if (RelationshipClassUtils.UsesRelationshipTable(relationshipClass))
			{
				// BUG in 9.3 SP1:
				// Geometries get assigned to wrong features if the order in table list
				// is not table, bridge-table, featureclass

				if (originFeatureClass != null && destinationFeatureClass != null)
				{
					// if two feature classes are involved, the one that 
					// the shape field is taken from must be first in the list

					baseFeatureClass = destinationFeatureClass;

					result.Add(destClassName);
					result.Add(relClassName);
					result.Add(originClassName);
				}
				else if (originFeatureClass != null)
				{
					baseFeatureClass = originFeatureClass;

					result.Add(destClassName);
					result.Add(relClassName);
					result.Add(originClassName);
				}
				else if (destinationFeatureClass != null)
				{
					baseFeatureClass = destinationFeatureClass;

					result.Add(originClassName);
					result.Add(relClassName);
					result.Add(destClassName);
				}
				else
				{
					// no feature class involved

					baseFeatureClass = null;

					result.Add(originClassName);
					result.Add(relClassName);
					result.Add(destClassName);
				}
			}
			else
			{
				// handle non-attributed relationships (i.e. fk is on destination table)
				switch (cardinality)
				{
					case esriRelCardinality.esriRelCardinalityOneToOne:
					case esriRelCardinality.esriRelCardinalityOneToMany:
						if (originFeatureClass != null && destinationFeatureClass != null)
						{
							baseFeatureClass = destinationFeatureClass;

							// TODO inverted. Not sure if appropriate for 1:n/1:1 relationships
							result.Add(destClassName);
							result.Add(originClassName);
						}
						else if (originFeatureClass != null)
						{
							baseFeatureClass = originFeatureClass;

							result.Add(originClassName);
							result.Add(destClassName);
						}
						else if (destinationFeatureClass != null)
						{
							baseFeatureClass = destinationFeatureClass;

							// TODO inverted. Not sure if appropriate for 1:n/1:1 relationships
							result.Add(destClassName);
							result.Add(originClassName);
						}
						else
						{
							// no feature class is involved
							baseFeatureClass = null;

							result.Add(originClassName);
							result.Add(destClassName);
						}

						break;

					default:
						throw CreateUnsupportedNonAttributedCardinalityException(cardinality);
				}
			}

			return result;
		}

		[NotNull]
		private IList<string> GetTableNames(
			[NotNull] IRelationshipClass relationshipClass,
			[NotNull] IFeatureClass baseFeatureClass,
			JoinType joinType,
			out int featureClassCount)
		{
			// BUG in 9.3 SP1 (NIM050134), also observed in 10.0 SP5:
			// Geometries get assigned to wrong features if the order in table list
			// is not table, bridge-table, featureclass. This is the case if "*" is used
			// as sub-fields. If an actual field list is used one has to ensure that the 
			// OBJECTID field of the non-shape table (or feature class which is not the base
			// feature class) does appear BEFORE the RID in the subfield list. Alternatively
			// the OBJECTID of the non-shape table can be excluded altogether.

			// Issue with table order: if both sides are FeatureClasses the table order must
			// be the following, otherwise "COMException (0x8004151B): Layer mismatch" occurs:
			// 1. Primary FeatureClass containing Shape field to be used
			// 2. Bridge table
			// 3. Secondary FeatureClass to be used as table only  
			// NIM050134 (or a similar behaviour in 10.0) must be avoided by ensuring that the
			// secondary feature class' OBJECTID appears before the RID.

			string originClassName = ((IDataset) relationshipClass.OriginClass).Name;
			string destClassName = ((IDataset) relationshipClass.DestinationClass).Name;
			string relClassName = ((IDataset) relationshipClass).Name;

			IList<string> result = new List<string>();

			featureClassCount = GetInvolvedFeatureClassCount(relationshipClass);

			Assert.True(featureClassCount > 0,
			            "At least one involved object class must be a feature class");

			if (featureClassCount == 1)
			{
				// let the other method overload determine the right order if a table is involved
				IFeatureClass usedFeatureClass;
				result = GetTableNames(relationshipClass, out usedFeatureClass,
				                       out featureClassCount);

				Assert.NotNull(usedFeatureClass, "No feature class found as base class in join.");
				Assert.True(DatasetUtils.IsSameObjectClass(usedFeatureClass, baseFeatureClass),
				            "Unexpected feature class determined as base feature class");
			}
			else // 2 feature classes
			{
				string primaryFeatureClassName = DatasetUtils.GetName(baseFeatureClass);

				if (originClassName == primaryFeatureClassName)
				{
					if (joinType == JoinType.RightJoin)
					{
						throw new InvalidOperationException(
							"Cannot use origin class (i.e. left table) {0} as base feature class in a right join");
					}

					Assert.False(joinType == JoinType.RightJoin, "");
					result.Add(originClassName);
					result.Add(relClassName);
					result.Add(destClassName);
				}
				else if (destClassName == primaryFeatureClassName)
				{
					if (joinType == JoinType.LeftJoin)
					{
						throw new InvalidOperationException(
							"Cannot use destination class (i.e. right table) {0} as base feature class in a left join");
					}

					result.Add(destClassName);
					result.Add(relClassName);
					result.Add(originClassName);
				}
				else
				{
					throw new InvalidOperationException(
						"The primary FeatureClass must be either the origin or destination.");
				}
			}

			return result;
		}

		#region Non-public members

		protected override string GetJoinCondition(JoinType joinType)
		{
			if (RelationshipClassUtils.UsesRelationshipTable(_relClass))
			{
				return GetBridgeTableJoinCondition(joinType);
			}

			// deal with non-attributed relationship classes

			esriRelCardinality cardinality = _relClass.Cardinality;

			switch (cardinality)
			{
				case esriRelCardinality.esriRelCardinalityOneToOne:
					return GetForeignKeyJoinCondition(joinType);

				case esriRelCardinality.esriRelCardinalityOneToMany:
					return GetForeignKeyJoinCondition(joinType);

				default:
					throw CreateUnsupportedNonAttributedCardinalityException(cardinality);
			}
		}

		protected override string GetTableJoinStatement(JoinType joinType,
		                                                bool ignoreFirstTable)
		{
			if (RelationshipClassUtils.UsesRelationshipTable(_relClass))
			{
				return GetBridgeTableJoinCondition(joinType, ignoreFirstTable);
			}

			// deal with non-attributed relationship classes

			esriRelCardinality cardinality = _relClass.Cardinality;

			switch (cardinality)
			{
				case esriRelCardinality.esriRelCardinalityOneToOne:
					return GetForeignKeyJoinCondition(joinType, ignoreFirstTable);

				case esriRelCardinality.esriRelCardinalityOneToMany:
					return GetForeignKeyJoinCondition(joinType, ignoreFirstTable);

				default:
					throw CreateUnsupportedNonAttributedCardinalityException(cardinality);
			}
		}

		private static int GetInvolvedFeatureClassCount(
			[NotNull] IRelationshipClass relationshipClass)
		{
			var originFeatureClass = relationshipClass.OriginClass as IFeatureClass;
			var destinationFeatureClass = relationshipClass.DestinationClass as IFeatureClass;

			int result = 0;

			if (originFeatureClass != null)
			{
				result++;
			}

			if (destinationFeatureClass != null)
			{
				result++;
			}

			return result;
		}

		private static bool IsInvolvedFeatureClass(
			[NotNull] IFeatureClass featureClass,
			[NotNull] IRelationshipClass relationshipClass)
		{
			if (DatasetUtils.IsSameObjectClass(featureClass, relationshipClass.OriginClass))
			{
				return true;
			}

			if (DatasetUtils.IsSameObjectClass(featureClass,
			                                   relationshipClass.DestinationClass))
			{
				return true;
			}

			return false;
		}

		private NotSupportedException CreateUnsupportedNonAttributedCardinalityException(
			esriRelCardinality cardinality)
		{
			return new NotSupportedException(
				string.Format(
					"Unsupported cardinality '{0}' for non-attributed relationship class: {1}",
					cardinality, DatasetUtils.GetName(_relClass)));
		}

		[NotNull]
		private JoinExpressionWriter GetJoinExpressionWriter()
		{
			if (_joinExpressionWriter == null)
			{
				IWorkspace workspace = ((IDataset) _relClass).Workspace;

				_joinExpressionWriter = CreateJoinExpressionWriter(workspace);
			}

			return _joinExpressionWriter;
		}

		[NotNull]
		private string GetForeignKeyJoinCondition(JoinType joinType)
		{
			return GetJoinCondition(GetOriginPK(), GetDestinationFK(), joinType);
		}

		[NotNull]
		private string GetBridgeTableJoinCondition(JoinType joinType)
		{
			string origFK = GetBridgeOriginFK();
			string destFK = GetBridgeDestinationFK();

			string origJoinCondition = GetJoinCondition(GetOriginPK(), origFK, joinType);
			string destJoinCondition = GetJoinCondition(destFK, GetDestinationPK(),
			                                            joinType);

			return string.Format("{0} AND {1}", origJoinCondition, destJoinCondition);
		}

		[NotNull]
		private string GetForeignKeyJoinCondition(JoinType joinType, bool ignoreFirstTable)
		{
			var sb = new StringBuilder();

			string firstTable;
			ITable joinTable;
			JoinType useJoin = joinType;
			if (joinType != JoinType.RightJoin)
			{
				firstTable = DatasetUtils.GetName(_relClass.OriginClass);
				joinTable = (ITable) _relClass.DestinationClass;
			}
			else
			{
				firstTable = DatasetUtils.GetName(_relClass.DestinationClass);
				joinTable = (ITable) _relClass.OriginClass;
				useJoin = JoinType.LeftJoin;
			}

			if (! ignoreFirstTable)
			{
				sb.AppendFormat(firstTable);
			}

			sb.Append(GetJoinTableExpression(useJoin, joinTable));
			sb.AppendFormat(" ON {0} = {1} ", GetOriginPK(), GetDestinationFK());

			return sb.ToString();
		}

		[NotNull]
		private string GetBridgeTableJoinCondition(JoinType joinType, bool ignoreFirstTable)
		{
			string origPK;
			string destPK;

			string origFK;
			string destFK;

			var sb = new StringBuilder();

			string firstTable;
			ITable joinTable;
			JoinType useJoin = joinType;
			if (joinType != JoinType.RightJoin)
			{
				firstTable = DatasetUtils.GetName(_relClass.OriginClass);
				joinTable = (ITable) _relClass.DestinationClass;

				origPK = GetOriginPK();
				destPK = GetDestinationPK();

				origFK = GetBridgeOriginFK();
				destFK = GetBridgeDestinationFK();
			}
			else
			{
				// invert statment
				useJoin = JoinType.LeftJoin;

				firstTable = DatasetUtils.GetName(_relClass.DestinationClass);
				joinTable = (ITable) _relClass.OriginClass;

				origPK = GetDestinationPK();
				destPK = GetOriginPK();

				origFK = GetBridgeDestinationFK();
				destFK = GetBridgeOriginFK();
			}

			if (! ignoreFirstTable)
			{
				sb.AppendFormat(firstTable);
			}

			sb.Append(GetJoinTableExpression(useJoin, (ITable) _relClass));
			sb.AppendFormat(" ON {0} = {1} ", origPK, origFK);

			sb.Append(GetJoinTableExpression(useJoin, joinTable));
			sb.AppendFormat(" ON {0} = {1} ", destFK, destPK);

			return sb.ToString();
		}

		[NotNull]
		private static string GetJoinTableExpression(JoinType joinType,
		                                             [NotNull] ITable table)
		{
			return string.Format(" {0} {1} ",
			                     GetSqlJoinExpression(joinType),
			                     DatasetUtils.GetName(table));
		}

		[NotNull]
		private static string GetSqlJoinExpression(JoinType joinType)
		{
			switch (joinType)
			{
				case JoinType.InnerJoin:
					return "INNER JOIN";

				case JoinType.LeftJoin:
					return "LEFT OUTER JOIN";

				case JoinType.RightJoin:
					return "RIGHT OUTER JOIN";

				default:
					throw new ArgumentOutOfRangeException(joinType.ToString());
			}
		}

		[NotNull]
		public string GetOriginPK()
		{
			string origPrimaryKey = _relClass.OriginPrimaryKey;

			return DatasetUtils.QualifyFieldName(_relClass.OriginClass, origPrimaryKey);
		}

		[NotNull]
		public string GetBridgeOriginFK()
		{
			string bridgeOriginFK = _relClass.OriginForeignKey;

			return DatasetUtils.QualifyFieldName((ITable) _relClass, bridgeOriginFK);
		}

		[NotNull]
		public string GetDestinationFK()
		{
			string destinationFK = _relClass.OriginForeignKey;

			return DatasetUtils.QualifyFieldName(_relClass.DestinationClass, destinationFK);
		}

		[NotNull]
		public string GetDestinationPK()
		{
			string destinationPK = _relClass.DestinationPrimaryKey;

			return DatasetUtils.QualifyFieldName(_relClass.DestinationClass, destinationPK);
		}

		[NotNull]
		public string GetBridgeDestinationFK()
		{
			string bridgeDestinationFK = _relClass.DestinationForeignKey;

			return DatasetUtils.QualifyFieldName((ITable) _relClass, bridgeDestinationFK);
		}

		[NotNull]
		private string GetJoinCondition([NotNull] string leftField,
		                                [NotNull] string rightField,
		                                JoinType joinType)
		{
			Assert.ArgumentNotNullOrEmpty(leftField, nameof(leftField));
			Assert.ArgumentNotNullOrEmpty(rightField, nameof(rightField));

			switch (joinType)
			{
				case JoinType.InnerJoin:
					return GetInnerJoinExpression(leftField, rightField);

				case JoinType.RightJoin:
				case JoinType.LeftJoin:
					return GetJoinExpressionWriter()
						.GetExpression(leftField, rightField, joinType);

				default:
					throw new ArgumentException("Unhandled join type: " + joinType);
			}
		}

		#endregion
	}
}
