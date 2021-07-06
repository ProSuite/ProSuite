using System.Collections.Generic;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.ManagedOptions;

namespace ProSuite.Commons.AO.Geometry.CreateFootprint
{
	public class PartialFootprintOptions : PartialOptionsBase
	{
		public PartialFootprintOptions()
		{
			SourceTargetMappings = new List<FootprintSourceTargetMapping>();
		}

		// NOTE: the value could be un-defined in the central settings file
		//		 - if it was deliberately deleted (meaning no default)
		//		 - for a new setting that has never been stored
		[CanBeNull]
		public OverridableSetting<double> ZOffset { get; set; }

		[CanBeNull]
		public OverridableSetting<bool> AutoFootprintMode { get; set; }

		[CanBeNull]
		public List<FootprintSourceTargetMapping> SourceTargetMappings { get; set; }

		[CanBeNull]
		public OverridableSetting<bool> UpdateZIfNoBuffer { get; set; }

		[CanBeNull]
		public OverridableSetting<bool> RelateExistingTargets { get; set; }

		public override PartialOptionsBase Clone()
		{
			var result = new PartialFootprintOptions();

			Assert.NotNull(result.SourceTargetMappings, "SourceTargetMapping not initialized");

			// deep copy of the list

			// so far the source-target mapping will be null in the local configuration
			if (SourceTargetMappings != null)
			{
				foreach (FootprintSourceTargetMapping mapping in SourceTargetMappings)
				{
					result.SourceTargetMappings.Add(
						new FootprintSourceTargetMapping(
							mapping.Source, mapping.Target, mapping.BufferDistanceFieldName,
							mapping.ZCalculationMethod, mapping.ZOffsetFieldName));
				}
			}

			result.ZOffset = TryClone(ZOffset);
			result.AutoFootprintMode = TryClone(AutoFootprintMode);

			result.RelateExistingTargets = TryClone(RelateExistingTargets);
			result.UpdateZIfNoBuffer = TryClone(UpdateZIfNoBuffer);

			return result;
		}
	}
}
