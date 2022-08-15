using System;
using System.Collections.Generic;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geodatabase.GdbSchema;
using ProSuite.Commons.Logging;
using ProSuite.QA.Container;

namespace ProSuite.QA.Tests.Transformers
{
	public abstract class TableTransformer<T> : InvolvesTablesBase, ITableTransformer<T>
		where T : IReadOnlyTable
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		private T _transformed;

		protected TableTransformer(IEnumerable<IReadOnlyTable> involvedTables)
			: base(involvedTables) { }

		private string _transformerName;

		public T GetTransformed()
		{
			if (_transformed == null)
			{
				try
				{
					T transformed = GetTransformedCore(_transformerName);

					UpdateConstraints(transformed);

					_transformed = transformed;
				}
				catch (Exception exception)
				{
					_msg.Debug($"Error creating {_transformerName}", exception);

					throw new InvalidOperationException(
						$"Cannot create TableTransformer {_transformerName}", exception);
				}
			}

			return _transformed;
		}

		private void UpdateConstraints(T transformed)
		{
			if (transformed is GdbTable gdbTable &&
			    gdbTable.BackingDataset is TransformedBackingData backingData)

				for (int i = 0; i < InvolvedTables.Count; i++)
				{
					backingData.SetConstraint(i, GetConstraint(i));
					backingData.SetSqlCaseSensitivity(i, GetSqlCaseSensitivity(i));
				}
		}

		protected abstract T GetTransformedCore(string tableName);

		object ITableTransformer.GetTransformed() => GetTransformed();

		string ITableTransformer.TransformerName
		{
			get => _transformerName;
			set => _transformerName = value;
		}

		#region Overrides of ProcessBase

		protected override void SetConstraintCore(IReadOnlyTable table, int tableIndex,
		                                          string constraint)
		{
			if (_transformed == null)
			{
				return;
			}

			UpdateConstraints(_transformed);
		}

		#endregion
	}
}
