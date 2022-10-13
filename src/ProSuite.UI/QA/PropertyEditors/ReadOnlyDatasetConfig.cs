using System.ComponentModel;
using System.Data;
using System.Drawing.Design;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.Core.DataModel;

namespace ProSuite.UI.QA.PropertyEditors
{
	public abstract class ReadOnlyDatasetConfig : DatasetConfig
	{
		[Editor(typeof(DatasetEditor), typeof(UITypeEditor))]
		[ReadOnly(true)]
		public override Dataset Data
		{
			get { return base.Data; }
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
