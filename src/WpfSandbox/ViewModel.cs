using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ProSuite.Commons.AGP.Carto;

namespace WpfSandbox
{
	class ViewModel
	{
		private TargetFeatureSelection _featureSelection;

		public TargetFeatureSelection FeatureSelection
		{
			get => _featureSelection;
			set => _featureSelection = value;
		}
	}
}
