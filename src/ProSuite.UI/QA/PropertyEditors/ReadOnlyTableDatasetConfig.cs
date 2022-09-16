using System.ComponentModel;
using System.Data;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.Core.DataModel;

namespace ProSuite.UI.QA.PropertyEditors
{
	public class ReadOnlyTableDatasetConfig : TableDatasetConfig
	{
		[UsedImplicitly]
		[ReadOnly(true)]
		public override Dataset Data
		{
			get { return base.Data; }
			set { throw new ReadOnlyException(); }
		}

		[UsedImplicitly]
		[ReadOnly(true)]
		public new string FilterExpression
		{
			get { return base.FilterExpression; }
			set { throw new ReadOnlyException(); }
		}

		[UsedImplicitly]
		[ReadOnly(true)]
		public override bool UsedAsReferenceData
		{
			get { return base.UsedAsReferenceData; }
			set { throw new ReadOnlyException(); }
		}
	}
}