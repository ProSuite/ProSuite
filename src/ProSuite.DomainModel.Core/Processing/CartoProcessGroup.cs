using System.Collections.Generic;
using System.Linq;
using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Validation;
using ProSuite.DomainModel.Core.Commands;
using ProSuite.DomainModel.Core.DataModel;

namespace ProSuite.DomainModel.Core.Processing
{
	public class CartoProcessGroup : VersionedEntityWithMetadata, INamed, IAnnotated
	{
		#region Fields

		[UsedImplicitly] private string _name;
		[UsedImplicitly] private string _description;
		[UsedImplicitly] private ICommandDescriptor _associatedCommand;
		[UsedImplicitly] private string _associatedCommandIcon;
		[UsedImplicitly] private CartoProcessType _associatedGroupProcessType;

		[UsedImplicitly] private readonly IList<CartoProcess> _processes =
			new List<CartoProcess>();

		#endregion

		#region Constructors

		/// <remarks>Required for NHibernate</remarks>
		public CartoProcessGroup() { }

		public CartoProcessGroup([NotNull] string name)
		{
			Assert.ArgumentNotNull(name, nameof(name));

			_name = name;
		}

		#endregion

		#region Properties

		#region INamed Members

		[Required]
		public string Name
		{
			get { return _name; }
			set { _name = value; }
		}

		#endregion

		#region IAnnotated Members

		public string Description
		{
			get { return _description; }
			set { _description = value; }
		}

		#endregion

		public ICommandDescriptor AssociatedCommand
		{
			get { return _associatedCommand; }
			set { _associatedCommand = value; }
		}

		/// <remarks>The icon used for the current CartoProcessGroup used
		/// for the <see cref="AssociatedCommand"/>, and in the chooser</remarks>
		public string AssociatedCommandIcon
		{
			get { return _associatedCommandIcon; }
			set { _associatedCommandIcon = value; }
		}

		/// <remarks>Tells with which Group CP implementation the current
		/// CartoProcessGroup is shown as group process and executed in the
		/// chooser (may be null if not shown as group process)</remarks>
		public CartoProcessType AssociatedGroupProcessType
		{
			get { return _associatedGroupProcessType; }
			set { _associatedGroupProcessType = value; }
		}

		public IList<CartoProcess> Processes
		{
			get { return new ReadOnlyList<CartoProcess>(_processes); }
		}

		#endregion

		#region Public

		public IEnumerable<CartoProcess> GetProcesses([CanBeNull] DdxModel model)
		{
			return _processes.Where(process => model == null || Equals(process.Model, model));
		}

		public void AddProcess([NotNull] CartoProcess process)
		{
			AddProcess(process, _processes.Count);
		}

		public void AddProcess([NotNull] CartoProcess process, int insertIndex)
		{
			Assert.ArgumentNotNull(process, nameof(process));

			_processes.Insert(insertIndex, process);
		}

		public void RemoveProcess([NotNull] CartoProcess process)
		{
			Assert.ArgumentNotNull(process, nameof(process));

			_processes.Remove(process);
		}

		public void Clear()
		{
			_processes.Clear();
		}

		public override string ToString()
		{
			return Name ?? string.Empty;
		}

		#endregion
	}
}
