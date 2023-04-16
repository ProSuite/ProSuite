using System.Collections.Generic;
using System.Linq;
using ProSuite.Commons.AO.Geodatabase.GdbSchema;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AO.Geodatabase
{
	public class JoinedRowFactory
	{
		[NotNull]
		protected GdbTable JoinedSchema { get; }

		private readonly IReadOnlyTable _geometryEndClass;
		private readonly IReadOnlyTable _otherEndClass;

		private IDictionary<int, int> _geometryEndCopyMatrix;
		private IDictionary<int, int> _otherEndCopyMatrix;
		private IDictionary<int, int> _associationTableCopyMatrix;

		public JoinedRowFactory(GdbTable joinedSchema,
		                        IReadOnlyTable geometryEndClass,
		                        IReadOnlyTable otherEndClass)
		{
			JoinedSchema = joinedSchema;
			_geometryEndClass = geometryEndClass;
			_otherEndClass = otherEndClass;
		}

		public IReadOnlyTable AssociationTable { get; set; }

		public GdbRow CreateRow(
			[NotNull] IReadOnlyRow leftRow,
			[CanBeNull] IReadOnlyRow otherRow,
			JoinSourceTable objectIdSource,
			[CanBeNull] IReadOnlyRow associationRow = null)
		{
			var joinedValueList = new MultiListValues();

			joinedValueList.AddRow(leftRow, GeometryEndCopyMatrix);
			joinedValueList.AddRow(otherRow, OtherEndCopyMatrix);

			if (associationRow != null)
			{
				// At least keep the original RID (ObjectId). Potentially it could also be an attributed m:n
				joinedValueList.AddRow(associationRow, AssociationTableCopyMatrix);
			}

			CreatingRowCore(joinedValueList, leftRow, otherRow);

			IReadOnlyRow oidSourceRow;
			if (objectIdSource == JoinSourceTable.Left)
			{
				oidSourceRow = leftRow;
			}
			else if (objectIdSource == JoinSourceTable.Right)
			{
				oidSourceRow = Assert.NotNull(otherRow);
			}
			else if (associationRow != null)
			{
				oidSourceRow = associationRow;
			}
			else
			{
				// Uniqueness is probably irrelevant, just use the left:
				oidSourceRow = leftRow;
			}

			GdbRow result = leftRow is IReadOnlyFeature &&
			                JoinedSchema is GdbFeatureClass gdbFeatureClass
				                ? GdbFeature.Create(oidSourceRow.OID, gdbFeatureClass, joinedValueList)
				                : new GdbRow(oidSourceRow.OID, JoinedSchema, joinedValueList);

			return result;
		}

		protected virtual void CreatingRowCore(
			[NotNull] MultiListValues joinedValueList,
			[NotNull] IReadOnlyRow leftRow,
			[CanBeNull] IReadOnlyRow otherRow) { }

		public IDictionary<int, int> GeometryEndCopyMatrix
		{
			get
			{
				if (_geometryEndCopyMatrix == null)
				{
					_geometryEndCopyMatrix =
						GdbObjectUtils.CreateMatchingIndexMatrix(
							              JoinedSchema, _geometryEndClass, true, true, null,
							              FieldComparison.FieldName).Where(pair => pair.Value >= 0)
						              .ToDictionary(pair => pair.Value, pair => pair.Key);
				}

				return _geometryEndCopyMatrix;
			}
			set { _geometryEndCopyMatrix = value; }
		}

		public IDictionary<int, int> OtherEndCopyMatrix
		{
			get
			{
				if (_otherEndCopyMatrix == null)
				{
					_otherEndCopyMatrix =
						GdbObjectUtils.CreateMatchingIndexMatrix(
							              JoinedSchema, _otherEndClass, true, true, null,
							              FieldComparison.FieldName).Where(pair => pair.Value >= 0)
						              .ToDictionary(pair => pair.Value, pair => pair.Key);
				}

				return _otherEndCopyMatrix;
			}
			set { _otherEndCopyMatrix = value; }
		}

		public IDictionary<int, int> AssociationTableCopyMatrix
		{
			get
			{
				if (_associationTableCopyMatrix == null)
				{
					var associationTable =
						Assert.NotNull(AssociationTable, "Association table not set");

					_associationTableCopyMatrix =
						GdbObjectUtils.CreateMatchingIndexMatrix(
							              JoinedSchema, associationTable, true, true, null,
							              FieldComparison.FieldName).Where(pair => pair.Value >= 0)
						              .ToDictionary(pair => pair.Value, pair => pair.Key);
				}

				return _associationTableCopyMatrix;
			}
			set { _associationTableCopyMatrix = value; }
		}
	}
}
