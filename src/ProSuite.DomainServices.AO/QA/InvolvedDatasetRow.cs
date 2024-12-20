using System;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.Core.DataModel;

namespace ProSuite.DomainServices.AO.QA
{
	public class InvolvedDatasetRow : IComparable, IComparable<InvolvedDatasetRow>
	{
		public InvolvedDatasetRow([NotNull] IObjectDataset dataset, long objectId)
		{
			Assert.ArgumentNotNull(dataset, nameof(dataset));

			Dataset = dataset;
			ObjectId = objectId;
		}

		[NotNull]
		public IObjectDataset Dataset { get; }

		public long ObjectId { get; }

		public int CompareTo(InvolvedDatasetRow other)
		{
			// TODO allow for Guids
			int oidComparison = ObjectId.CompareTo(other.ObjectId);
			if (oidComparison != 0)
			{
				return oidComparison;
			}

			return string.Compare(Dataset.Name, other.Dataset.Name, StringComparison.Ordinal);
		}

		public int CompareTo(object obj)
		{
			var involvedDatasetRow = obj as InvolvedDatasetRow;
			return involvedDatasetRow == null
				       ? -1
				       : CompareTo(involvedDatasetRow);
		}

		public override string ToString()
		{
			return $"Dataset: {Dataset.Name}, ObjectId: {ObjectId}";
		}
	}
}
