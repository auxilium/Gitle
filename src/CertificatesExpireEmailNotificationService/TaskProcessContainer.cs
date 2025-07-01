using Castle.Core.Resource;
using Castle.Facilities.Logging;
using Castle.MicroKernel.Registration;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.Services.Logging.Log4netIntegration;
using Castle.Windsor;
using Castle.Windsor.Configuration.Interpreters;
using Castle.Windsor.Installer;
using FluentNHibernate.Cfg;
using FluentNHibernate.Cfg.Db;
using Gitle.Model;
using Gitle.Model.Interfaces.Service;
using Gitle.Model.Mapping;
using Gitle.Service;
using NHibernate;
using NHibernate.Context;

namespace CertificatesExpireEmailNotificationService
{
  public class TaskProcessContainer : WindsorContainer
  {
    public TaskProcessContainer() : base(new XmlInterpreter(new ConfigResource()))
    {
      Install(FromAssembly.This());
      Install(Configuration.FromAppConfig());

      // Register the scheduler
      Register(
        Component.For<DailyCertificateCheck>()
          .Named("dailyCertificateCheck")
          .LifestyleSingleton()
      );
    }

    public class LogInstaller : IWindsorInstaller
    {
      public void Install(IWindsorContainer container, IConfigurationStore store)
      {
        container.AddFacility<LoggingFacility>(facility =>
          facility.LogUsing<Log4netFactory>()
            .WithConfig("log4net.config")
        );
      }
    }
    public class EmailServiceInstaller : IWindsorInstaller
    {
      public void Install(IWindsorContainer container, IConfigurationStore store)
      {
        container.Register(
          Component.For<IEmailService>()
            .ImplementedBy<EmailService>()
            .LifestyleSingleton()
            .DependsOn(Dependency.OnValue("sourceAddress", "noreply@auxilium.nl"))
            .DependsOn(Dependency.OnValue("bccAddresses", new string[0]))
            .DependsOn(Dependency.OnValue("testMode", false))
        );
      }
    }

    public class SessionFactoryInstaller : IWindsorInstaller
    {
      public void Install(IWindsorContainer container, IConfigurationStore store)
      {
        var sessionFactory = Fluently.Configure()
          .Database(MsSqlConfiguration.MsSql2008
            .ConnectionString(builder => builder.FromConnectionStringWithKey("Gitle")))
          .ExposeConfiguration(configuration =>
            configuration.CurrentSessionContext<ThreadLocalSessionContext>())
          .Mappings(configuration =>
            configuration.FluentMappings.AddFromAssemblyOf<ModelBaseMap<ModelBase>>())
          .BuildSessionFactory();

        container.Register(
          Component.For<ISessionFactory>()
            .Instance(sessionFactory)
            .LifestyleSingleton()
        );
      }
    }
  }
}
