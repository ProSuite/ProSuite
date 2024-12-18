using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ProSuite.Commons.AGP.Core.GeometryProcessing;
using ProSuite.Commons.ManagedOptions;

namespace ProSuite.AGP.Editing {
	public class TargetFeatureSelectionViewModel
	{
		public TargetFeatureSelectionViewModel(CentralizableSetting<TargetFeatureSelection> centralizableSetting)
		{
			CentralizableSetting = centralizableSetting;
			
		}

		public CentralizableSetting<TargetFeatureSelection> CentralizableSetting { get; }

		public TargetFeatureSelection CurrentValue
		{
			get { return CentralizableSetting.CurrentValue; }
			set => CentralizableSetting.CurrentValue = value;
		}
	}
}
