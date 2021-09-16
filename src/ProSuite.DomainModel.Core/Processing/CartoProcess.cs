using System.Collections.Generic;
using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Validation;
using ProSuite.DomainModel.Core.DataModel;

namespace ProSuite.DomainModel.Core.Processing
{
	public class CartoProcess : VersionedEntityWithMetadata, INamed, IAnnotated
	{
		#region Fields

		[UsedImplicitly] private string _name;
		[UsedImplicitly] private string _description;
		[UsedImplicitly] private DdxModel _model;
		[UsedImplicitly] private CartoProcessType _cartoProcessType;

		[UsedImplicitly] private readonly IList<CartoProcessParameter> _parameters =
			new List<CartoProcessParameter>();

		#endregion

		#region Constructors

		/// <remarks>Required for NHibernate</remarks>
		public CartoProcess() { }

		public CartoProcess([NotNull] string name, [CanBeNull] string description,
		                    [NotNull] DdxModel model,
		                    [NotNull] CartoProcessType cartoProcessType)
		{
			Assert.ArgumentNotNull(name, nameof(name));
			Assert.ArgumentNotNull(model, nameof(model));
			Assert.ArgumentNotNull(cartoProcessType, nameof(cartoProcessType));

			_name = name;
			_description = description;
			_model = model;
			_cartoProcessType = cartoProcessType;
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

		[Required]
		public DdxModel Model
		{
			get { return _model; }
			set { _model = value; }
		}

		[Required]
		public CartoProcessType CartoProcessType
		{
			get { return _cartoProcessType; }
			set { _cartoProcessType = value; }
		}

		public IList<CartoProcessParameter> Parameters =>
			new ReadOnlyList<CartoProcessParameter>(_parameters);

		#endregion

		#region Public

		public void AddParameter([NotNull] CartoProcessParameter parameter)
		{
			AddParameter(parameter, _parameters.Count);
		}

		public void AddParameter([NotNull] CartoProcessParameter parameter, int insertIndex)
		{
			Assert.ArgumentNotNull(parameter, nameof(parameter));

			_parameters.Insert(insertIndex, parameter);
		}

		public void RemoveParameter([NotNull] CartoProcessParameter parameter)
		{
			Assert.ArgumentNotNull(parameter, nameof(parameter));

			_parameters.Remove(parameter);
		}

		public void ClearParameters()
		{
			_parameters.Clear();
		}

		[NotNull]
		public CartoProcess CreateCopy()
		{
			var copy = new CartoProcess();

			CopyProperties(copy);

			return copy;
		}

		public override string ToString()
		{
			return Name ?? string.Empty;
		}

		#endregion

		#region Private

		private void CopyProperties([NotNull] CartoProcess target)
		{
			Assert.ArgumentNotNull(target, nameof(target));

			target._name = Name;
			target._description = Description;
			target._model = Model;
			target._cartoProcessType = CartoProcessType;

			if (Parameters.Count == 0)
			{
				return;
			}

			target.ClearParameters();
			foreach (CartoProcessParameter parameter in Parameters)
			{
				target.AddParameter(new CartoProcessParameter(parameter.Name, parameter.Value,
				                                              target));
			}
		}

		#endregion
	}
}
