using ProSuite.Commons.Essentials.Assertions;
using ProSuite.DomainModel.AO.DataModel;
using ProSuite.DomainModel.AO.Workflow;
using ProSuite.Microservices.Definitions.Shared.Ddx;

namespace ProSuite.Microservices.AO
{
	public static class ProtobufUtils
	{
		/// <summary>
		/// Return null, if the specified string is empty (i.e. the default value for string
		/// protocol buffers), or the input string otherwise.
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>
		public static string EmptyToNull(string value)
		{
			return string.IsNullOrEmpty(value) ? null : value;
		}

		public static ProjectMsg ToProjectMsg<T>(Project<T> project) where T : ProductionModel
		{
			var result = new ProjectMsg();

			result.ProjectId = project.Id;
			result.Name = Assert.NotNullOrEmpty(project.Name);
			result.Description = project.Description ?? string.Empty;
			result.ShortName = Assert.NotNullOrEmpty(project.ShortName);
			result.ModelId = project.ProductionModel?.Id ?? -1;

			result.MinimumScaleDenominator = project.MinimumScaleDenominator;
			result.ExcludeReadOnlyDatasetsFromProjectWorkspace =
				project.ExcludeReadOnlyDatasetsFromProjectWorkspace;
			result.AttributeEditorConfigDir =
				project.AttributeEditorConfigDirectory ?? string.Empty;
			result.ToolConfigDirectory = project.ToolConfigDirectory ?? string.Empty;
			result.WorkListConfigDir = project.WorkListConfigDirectory ?? string.Empty;

			result.FullExtentXMin = project.FullExtentXMin ?? double.NaN;
			result.FullExtentYMin = project.FullExtentYMin ?? double.NaN;
			result.FullExtentXMax = project.FullExtentXMax ?? double.NaN;
			result.FullExtentYMax = project.FullExtentYMax ?? double.NaN;

			return result;
		}
	}
}
