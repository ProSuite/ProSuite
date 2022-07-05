using System.Linq;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Misc;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.QA;

namespace ProSuite.DdxEditor.Content.Blazor.ViewModel
{
	internal static class DatasetParameterViewModelUtils
	{
		internal static string GetModelName(
			[NotNull] Either<Dataset, TransformerConfiguration> sourceValue)
		{
			return sourceValue.Match(d => d?.Model?.Name, GetModelName);
		}

		[CanBeNull]
		internal static string GetModelName(TransformerConfiguration transformer)
		{
			if (transformer == null)
			{
				return null;
			}

			var distinctModels = transformer.GetDatasetParameterValues(false, true)
			                                .Select(d => d.Model)
			                                .Distinct().ToList();

			if (distinctModels.Count == 0)
			{
				return null;
			}
			else if (distinctModels.Count == 1)
			{
				return distinctModels[0].Name;
			}
			else
			{
				return "<multiple>";
			}
		}
	}
}
