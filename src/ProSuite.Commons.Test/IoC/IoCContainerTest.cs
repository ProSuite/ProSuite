using NUnit.Framework;
using ProSuite.Commons.IoC;
using ProSuite.Commons.Notifications;

namespace ProSuite.Commons.Test.IoC
{
	[TestFixture]
	public class IoCContainerTest
	{
		[Test]
		public void CanRegisterComponent()
		{
			IoCContainer container = new IoCContainer();

			var component = new Notification("bla");
			container.Register<INotification>(component);

			INotification resolved = container.Resolve<INotification>();

			Assert.NotNull(resolved);
			Assert.IsTrue(resolved == component);
		}

		[Test]
		public void ThrowsIfComponentNotFound()
		{
			IoCContainer container = new IoCContainer();

			Assert.Throws<ComponentNotFoundException>(
				() => container.Resolve<INotification>());

			Assert.Throws<ComponentNotFoundException>(
				() => container.Resolve<INotification>("does not exist"));
		}

		[Test]
		public void CanRegisterNamedComponent()
		{
			IoCContainer container = new IoCContainer();

			// Register another, un-named component
			var bliComponent = new Notification("bli");
			container.Register<INotification>(bliComponent);

			// Register named
			var blaComponent = new Notification("bla");
			container.Register<INotification>(blaComponent, "bla_component");

			INotification resolved = container.Resolve<INotification>("bla_component");

			Assert.NotNull(resolved);
			Assert.IsTrue(resolved == blaComponent);
		}

		[Test]
		public void CanRegisterNamedTransientComponent()
		{
			IoCContainer container = new IoCContainer();

			// Register another, un-named transient component
			container.Register<INotification>(() => new Notification("bli"));

			// Register named
			var blaComponent = new Notification("bla");
			container.Register<INotification>(
				() => new Notification("bla"), "bla_component");

			INotification resolvedBla = container.Resolve<INotification>("bla_component");

			Assert.NotNull(resolvedBla);
			Assert.IsFalse(resolvedBla == blaComponent);
			Assert.AreEqual("bla", resolvedBla.Message);

			INotification resolvedBli1 = container.Resolve<INotification>();
			INotification resolvedBli2 = container.Resolve<INotification>();

			Assert.NotNull(resolvedBli1);
			Assert.NotNull(resolvedBli2);
			Assert.IsFalse(resolvedBli1 == resolvedBli2);
			Assert.AreEqual("bli", resolvedBli1.Message);
			Assert.AreEqual("bli", resolvedBli2.Message);
		}
	}
}
