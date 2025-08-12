using System;
using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geodatabase.GdbSchema;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Commons.Text;
using ProSuite.QA.Container;
using ProSuite.QA.Core;
using ProSuite.QA.Core.TestCategories;
using ProSuite.QA.Tests.Documentation;

namespace ProSuite.QA.Tests.Transformers
{
	[UsedImplicitly]
	[TableTransformer]
	public class TrTableJoinInMemory : TableTransformer<GdbTable>, ITableTransformerFieldSettings
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		private readonly IReadOnlyTable _leftTable;
		private readonly IReadOnlyTable _rightTable;
		private readonly string _leftTableKey;
		private readonly string _rightTableKey;

		private IReadOnlyTable _manyToManyTable;
		private string _manyToManyTableLeftKey;
		private string _manyToManyTableRightKey;

		private readonly JoinType _joinType;

		private GdbTable _joinedTable;

		[DocTr(nameof(DocTrStrings.TrTableJoinInMemory_0))]
		public TrTableJoinInMemory(
			[NotNull] [DocTr(nameof(DocTrStrings.TrTableJoinInMemory_leftTable))]
			IReadOnlyTable leftTable,
			[NotNull] [DocTr(nameof(DocTrStrings.TrTableJoinInMemory_rightTable))]
			IReadOnlyTable rightTable,
			[NotNull] [DocTr(nameof(DocTrStrings.TrTableJoinInMemory_leftTableKey))]
			string leftTableKey,
			[NotNull] [DocTr(nameof(DocTrStrings.TrTableJoinInMemory_rightTableKey))]
			string rightTableKey,
			[DocTr(nameof(DocTrStrings.TrTableJoinInMemory_joinType))]
			JoinType joinType)
			: base(new[] { leftTable, rightTable })
		{
			_leftTable = leftTable;
			_rightTable = rightTable;

			_leftTableKey = leftTableKey ?? throw new ArgumentNullException(nameof(leftTableKey));
			_rightTableKey = rightTableKey;

			_joinType = joinType;
		}

		[TestParameter]
		[CanBeNull]
		[DocTr(nameof(DocTrStrings.TrTableJoinInMemory_manyToManyTable))]
		public IReadOnlyTable ManyToManyTable
		{
			get => _manyToManyTable;
			set
			{
				_manyToManyTable = value;

				if (value != null)
				{
					// Does queriedOnly mean anything in case of transformers?
					AddInvolvedTable(_manyToManyTable, null, false, true);
				}
			}
		}

		[TestParameter]
		[CanBeNull]
		[DocTr(nameof(DocTrStrings.TrTableJoinInMemory_manyToManyTableLeftKey))]
		public string ManyToManyTableLeftKey
		{
			get => _manyToManyTableLeftKey;
			set => _manyToManyTableLeftKey = StringUtils.IsNullOrEmptyOrBlank(value) ? null : value;
		}

		[TestParameter]
		[CanBeNull]
		[DocTr(nameof(DocTrStrings.TrTableJoinInMemory_manyToManyTableRightKey))]
		public string ManyToManyTableRightKey
		{
			get => _manyToManyTableRightKey;
			set => _manyToManyTableRightKey =
				       StringUtils.IsNullOrEmptyOrBlank(value) ? null : value;
		}
		
		#region Implementation of ITableTransformer

		protected override GdbTable GetTransformedCore(string name)
		{
			if (_joinedTable == null)
			{
				AssociationDescription association = CreateAssociationDescription();

				const bool ensureUniqueIds = true;

				_joinedTable = CreateJoinedGdbTable(
					association, _leftTable, name, ensureUniqueIds, _joinType);

				JoinedBackingDataset joinedDataset =
					(JoinedBackingDataset) Assert.NotNull(_joinedTable.BackingDataset);

				AddFields(association, joinedDataset, _joinedTable, null,
				          null, FullyQualifyFieldNames);

				// Once the schema is created, we can set up the field-lookups:
				joinedDataset.SetupRowFactory();
			}

			return _joinedTable;
		}

		#endregion

		#region Implementation of ITableTransformerFieldSettings

		/// <summary>
		/// Whether all field names in the output table should be fully qualified using the
		/// SourceTableName.FieldName convention.
		/// </summary>
		public bool FullyQualifyFieldNames { get; set; }

		#endregion

		/// <summary>
		/// Creates a GdbTable or GdbFeatureClass representing the join of the specified association.
		/// </summary>
		/// <param name="associationDescription">The definition of the join</param>
		/// <param name="geometryTable">The 'left' table which, if it has a geometry field, will
		/// be used to define the resulting FeatureClass.</param>
		/// <param name="name">The name of the result class.</param>
		/// <param name="ensureUniqueIds">Whether some extra performance penalty should be accepted
		/// in order to guarantee the uniqueness of the OBJECTID field. This is relevant for
		/// many-to-many joins.</param>
		/// <param name="joinType"></param>
		/// <returns></returns>
		/// <exception cref="NotImplementedException"></exception>
		private static GdbTable CreateJoinedGdbTable(
			[NotNull] AssociationDescription associationDescription,
			[CanBeNull] IReadOnlyTable geometryTable,
			[NotNull] string name,
			bool ensureUniqueIds,
			JoinType joinType = JoinType.InnerJoin)
		{
			// TODO: Support both tables having no geometry -> get all records from the smaller?
			//       Or possibly always query the left table first (in inner joins) to allow for optimization by user.
			//       support left table having the geometry but using right join -> swap, leftJoin
			if (joinType == JoinType.RightJoin)
			{
				throw new NotImplementedException("RightJoin is not yet implemented.");
			}

			// If the geometry table is null, the 'left' table will be used (if it has a geometry).
			geometryTable = geometryTable ?? associationDescription.Table1;

			IReadOnlyTable otherTable = associationDescription.Table1.Equals(geometryTable)
				                            ? associationDescription.Table2
				                            : associationDescription.Table1;

			JoinedBackingDataset BackingDatasetFactoryFunc(GdbTable joinedSchema)
			{
				return new JoinedBackingDataset(
					       associationDescription, geometryTable, otherTable, joinedSchema)
				       {
					       JoinType = joinType,
				       };
			}

			var result = CreateJoinedGdbTable(name, BackingDatasetFactoryFunc, geometryTable);

			return result;
		}

		private static void AddFields([NotNull] AssociationDescription association,
		                              [NotNull] JoinedBackingDataset joinedDataset,
		                              [NotNull] GdbTable resultTable,
		                              [CanBeNull] IList<string> leftTableAttributes,
		                              [CanBeNull] IList<string> rightTableAttributes,
		                              bool fullyQualifyFields = false)
		{
			// Suggestion for multi-table transformers: fields are only qualified to avoid duplicates
			// using <input table name>_

			// Use already wrapped instances for correct equality comparison!
			var leftTable = joinedDataset.LeftTable;
			var rightTable = joinedDataset.RightTable;
			var associationTable = joinedDataset.AssociationTable;

			TransformedTableFields leftFields =
				new TransformedTableFields(leftTable)
				{
					QualifyFieldsWithSourceTable = fullyQualifyFields
				};

			TransformedTableFields rightFields =
				new TransformedTableFields(rightTable)
				{
					QualifyFieldsWithSourceTable = fullyQualifyFields
				};

			rightFields.ExcludeAllShapeFields();

			joinedDataset.TableFieldsBySource.Add(leftTable, leftFields);
			joinedDataset.TableFieldsBySource.Add(rightTable, rightFields);

			TransformedTableFields bridgeTableFields = null;

			// In case of m:n ensure the field is in the result schema:
			if (associationTable != null)
			{
				bridgeTableFields = new TransformedTableFields(associationTable);
				joinedDataset.TableFieldsBySource.Add(associationTable, bridgeTableFields);
			}

			IReadOnlyTable oidSourceTable =
				TableJoinUtils.DetermineOIDTable(association, joinedDataset.JoinType, leftTable);

			if (oidSourceTable != null && oidSourceTable.HasOID)
			{
				// It must be qualified in order to find out the source table name
				string qualifiedOidName = DatasetUtils.QualifyFieldName(oidSourceTable,
					oidSourceTable.OIDFieldName);
				joinedDataset.TableFieldsBySource[oidSourceTable].AddOIDField(resultTable,
					qualifiedOidName);
			}

			if (leftTableAttributes != null)
			{
				leftFields.AddUserDefinedFields(leftTableAttributes, resultTable);
			}
			else
			{
				leftFields.AddAllFields(resultTable);
			}

			rightFields.PreviouslyAddedFields.Add(leftFields);

			if (rightTableAttributes != null)
			{
				rightFields.AddUserDefinedFields(rightTableAttributes, resultTable);
			}
			else
			{
				rightFields.AddAllFields(resultTable);
			}

			bridgeTableFields?.PreviouslyAddedFields.Add(rightFields);
			bridgeTableFields?.AddAllFields(resultTable);
		}

		private AssociationDescription CreateAssociationDescription()
		{
			AssociationDescription association;

			if (ManyToManyTable == null &&
			    string.IsNullOrEmpty(_manyToManyTableLeftKey) &&
			    string.IsNullOrEmpty(_manyToManyTableRightKey))
			{
				association = new ForeignKeyAssociationDescription(
					_leftTable, _leftTableKey, _rightTable, _rightTableKey);
			}
			else
			{
				if (ManyToManyTable == null ||
				    string.IsNullOrEmpty(_manyToManyTableLeftKey) ||
				    string.IsNullOrEmpty(_manyToManyTableRightKey))
				{
					throw new ArgumentNullException(
						$"Many-to-many attributes ({nameof(ManyToManyTable)}, " +
						$"{nameof(ManyToManyTableLeftKey)}, {nameof(ManyToManyTableRightKey)}) " +
						"must all be null or all specified.");
				}

				association = new ManyToManyAssociationDescription(
					_leftTable, _leftTableKey, _rightTable, _rightTableKey,
					_manyToManyTable, _manyToManyTableLeftKey, _manyToManyTableRightKey);
			}

			_msg.DebugFormat("Creating join {0} using {1}", TransformerName, association);

			return association;
		}

		private static GdbTable CreateJoinedGdbTable(
			[NotNull] string name,
			[NotNull] Func<GdbTable, JoinedBackingDataset> datasetFactoryFunc,
			IReadOnlyTable geometryEndClass)
		{
			GdbTable result;

			IWorkspace workspace = geometryEndClass.Workspace;

			if (geometryEndClass is IReadOnlyFeatureClass featureClass)
			{
				result = new TransformedFeatureClass<JoinedBackingDataset>(
					         null, name, featureClass.ShapeType, datasetFactoryFunc, workspace)
				         {
					         NoCaching = false
				         };
			}
			else
			{
				result = new TransformedTable<JoinedBackingDataset>(
					null, name, datasetFactoryFunc, workspace);
			}

			return result;
		}
	}
}
