using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Validation;

namespace ProSuite.DomainModel.Core.Processing
{
	public class CartoProcessParameter : EntityWithMetadata
	{
		#region Fields

		[UsedImplicitly] private readonly string _name;
		[UsedImplicitly] private string _value;
		[UsedImplicitly] private readonly CartoProcess _cartoProcess;

		#endregion

		#region Constructors

		/// <remarks>Required for NHibernate</remarks>
		protected CartoProcessParameter() { }

		public CartoProcessParameter([NotNull] string name, [CanBeNull] string value,
		                             [NotNull] CartoProcess cartoProcess)
		{
			Assert.ArgumentNotNull(name, nameof(name));
			Assert.ArgumentNotNull(cartoProcess, nameof(cartoProcess));

			_name = name;
			_value = value;
			_cartoProcess = cartoProcess;
		}

		#endregion

		#region Properties

		[Required]
		public string Name
		{
			get { return _name; }
		}

		public string Value
		{
			get { return _value; }
			set { _value = value; }
		}

		[Required]
		public CartoProcess CartoProcess
		{
			get { return _cartoProcess; }
		}

		#endregion

		public override string ToString()
		{
			return string.Format("{0} (value={1}, cartoProcess={2})",
			                     _name ?? "<no name>", _value ?? "<no value>",
			                     _cartoProcess?.Name ?? "<no cartoProcess>");
		}
	}
}
