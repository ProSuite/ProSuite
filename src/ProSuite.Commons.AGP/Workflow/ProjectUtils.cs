using System;
using System.Linq;
using System.Threading.Tasks;
using ArcGIS.Desktop.Core;
using ArcGIS.Desktop.Core.UnitFormats;

namespace ProSuite.Commons.AGP.Workflow;

public static class ProjectUtils
{
	/// <summary>
	/// Create and open a new Untitled ArcGIS Pro project
	/// </summary>
	/// <returns>The new project</returns>
	public static async Task<Project> CreateUntitledProject()
	{
		var settings = new CreateProjectSettings
		               {
			               TemplatePath = null,
			               TemplateType = TemplateType.Untitled
		               };

		var project = await Project.CreateAsync(settings);

		return project;
	}

	/// <summary>
	/// Get the default units for the current project
	/// (as can be set in ArcGIS Pro through Project/Options/Units).
	/// </summary>
	/// <remarks>Must call on MCT</remarks>
	public static DisplayUnitFormat[] GetDefaultProjectUnits()
	{
		var types = Enum.GetValues<UnitFormatType>();
		var formats = DisplayUnitFormats.Instance;
		return types.Select(type => formats.GetDefaultProjectUnitFormat(type))
		            .ToArray();
	}

	/// <summary>
	/// Set current project's default symbol (2D) units, either by
	/// <paramref name="abbreviation"/> or by <paramref name="name"/>.
	/// As of ArcGIS Pro 3.5, available symbol (2D) units are:
	/// pt (Point), mm (Millimeter), cm (Centimeter), in (Inch)
	/// </summary>
	/// <returns>true iff the unit was found and set as default</returns>
	/// <remarks>Must run on MCT</remarks>
	public static bool SetDefaultProjectSymbolUnit(string abbreviation, string name = null)
	{
		return SetDefaultProjectUnit(UnitFormatType.Symbol2D, abbreviation, name);
	}

	public static bool SetDefaultProjectUnit(UnitFormatType type, string abbreviation, string name = null)
	{
		var formats = DisplayUnitFormats.Instance;

		var units = formats.GetPredefinedProjectUnitFormats(type).AsEnumerable();

		if (!string.IsNullOrEmpty(abbreviation))
		{
			units = units.Where(u => u.Abbreviation == abbreviation);
		}

		if (!string.IsNullOrEmpty(name))
		{
			const StringComparison comparison = StringComparison.OrdinalIgnoreCase;
			units = units.Where(u =>
				                    string.Equals(u.UnitName, name, comparison) ||
				                    string.Equals(u.DisplayName, name, comparison) ||
				                    string.Equals(u.DisplayNamePlural, name, comparison));
		}

		var unit = units.FirstOrDefault();
		if (unit is null)
		{
			return false; // no such unit found
		}

		formats.SetDefaultProjectUnitFormat(unit);
		return true;
	}
}
