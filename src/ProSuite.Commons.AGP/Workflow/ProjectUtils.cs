using System;
using System.IO;
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
	/// Add a folder connection to the project
	/// </summary>
	/// <returns>The added project item</returns>
	/// <remarks>Must run on MCT</remarks>
	public static IProjectItem AddFolderConnection(Project project, string folderPath)
	{
		if (string.IsNullOrEmpty(folderPath))
			throw new ArgumentNullException(nameof(folderPath));

		if (! Directory.Exists(folderPath))
			throw new ArgumentException($"Not a directory: {folderPath}", nameof(folderPath));

		Item item = ItemFactory.Instance.Create(folderPath);

		return AddProjectItem(project, item);
	}

	/// <summary>
	/// Add a database connection (.gdb or .sde) to the project
	/// </summary>
	/// <returns>The added project item</returns>
	/// <remarks>Must run on MCT</remarks>
	public static IProjectItem AddDatabaseConnection(Project project, string databasePath)
	{
		if (string.IsNullOrEmpty(databasePath))
			throw new ArgumentNullException(nameof(databasePath));

		Item item = ItemFactory.Instance.Create(databasePath);

		if (item is null)
		{
			// empirical: if databasePath does not exist, ItemFactory returns null
			throw new InvalidOperationException(
				$"ItemFactory.Instance.Create() returned null for path: {databasePath}");
		}

		return AddProjectItem(project, item);
	}

	/// <remarks>Must run on MCT</remarks>
	private static IProjectItem AddProjectItem(Project project, Item item)
	{
		if (project is null)
			throw new ArgumentNullException(nameof(project));
		if (item is null)
			throw new ArgumentNullException(nameof(item));

		// About item.Name/Title/Alias:
		// Prefer item.Name for messages (item.Title tends to be empty and item.Alias is under the user's control)

		var projectItem = item as IProjectItem ??
		                  throw new InvalidOperationException(
			                  $"Item {item.Name} is not {nameof(IProjectItem)}, cannot add to project");

		// Beware: Project.FindItem(path) searches some ItemFactory.Instance.Cache
		// and NOT items actually added to the project! Use Project.GetItems instead

		bool ok = project.AddItem(projectItem); // must run on MCT

		if (! ok)
		{
			var existing = project.GetItems<Item>().FirstOrDefault(i => string.Equals(i.Path, item.Path));
			if (existing is null)
			{
				throw new InvalidOperationException($"AddItem({item.Name}) returned false");
			}
			// else: item already existed in project, good
		}

		return projectItem;
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
