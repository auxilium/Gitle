namespace Gitle.Web
{
    #region Usings

    using Castle.Core.Resource;
    using Castle.Facilities.Logging;
    using Castle.MicroKernel.Registration;
    using Castle.MicroKernel.SubSystems.Configuration;
    using Castle.MonoRail.Framework;
    using Castle.MonoRail.WindsorExtension;
    using Castle.Services.Logging.Log4netIntegration;
    using Castle.Windsor;
    using Castle.Windsor.Configuration.Interpreters;
    using Castle.Windsor.Installer;
    using Clients.GitHub;
    using Clients.GitHub.Interfaces;
    using Model.Interfaces.Service;
    using Service;

    #endregion

    public class GitleContainer : WindsorContainer
    {
        public GitleContainer()
            : base(new XmlInterpreter(new ConfigResource()))
        {
            Install(FromAssembly.This());
        }

        public class MonorailInstaller : IWindsorInstaller
        {
            public void Install(IWindsorContainer container, IConfigurationStore store)
            {
                container.AddFacility<MonoRailFacility>();

                container.Register(Classes.FromThisAssembly()
                                       .Where(c => typeof (IController).IsAssignableFrom(c)
                                                   || typeof (IFilter).IsAssignableFrom(c)
                                                   || typeof (ViewComponent).IsAssignableFrom(c))
                                       .LifestylePerWebRequest());
            }
        }

        public class LoggerInstaller : IWindsorInstaller
        {
            public void Install(IWindsorContainer container, IConfigurationStore store)
            {
                container.AddFacility<LoggingFacility>(f => f.LogUsing<Log4netFactory>());
            }
        }

        public class SettingInstaller : IWindsorInstaller
        {
            public void Install(IWindsorContainer container, IConfigurationStore store)
            {
                container.Register(Component.For<ISettingService>().ImplementedBy(typeof(SettingService)).LifestyleSingleton());
            }
        }

        public class JamesRegistrationInstaller : IWindsorInstaller
        {
            public void Install(IWindsorContainer container, IConfigurationStore store)
            {
                container.Register(Component.For<IJamesRegistrationService>().ImplementedBy(typeof(JamesRegistrationService)).LifestylePerWebRequest());
            }
        }

        public class ClientsInstaller : IWindsorInstaller
        {
            public void Install(IWindsorContainer container, IConfigurationStore store)
            {
                container.Register(Classes.FromAssemblyNamed("Gitle.Clients.GitHub")
                                       .Where(type => type.Name.EndsWith("Client"))
                                       .WithService.DefaultInterfaces()
                                       .LifestylePerWebRequest().Configure(
                                           registration =>
                                           registration.DependsOn(Dependency.OnAppSettingsValue("token", "token")).
                                               DependsOn(Dependency.OnAppSettingsValue("useragent", "useragent")).
                                               DependsOn(Dependency.OnAppSettingsValue("githubApi", "githubApi"))));
                container.Register(Classes.FromAssemblyNamed("Gitle.Clients.Freckle")
                                       .Where(type => type.Name.EndsWith("Client"))
                                       .WithService.DefaultInterfaces()
                                       .LifestylePerWebRequest().Configure(
                                           registration =>
                                           registration.DependsOn(Dependency.OnAppSettingsValue("freckleApi", "freckleApi")).
                                               DependsOn(Dependency.OnAppSettingsValue("token", "freckleToken"))));
            }
        }
    }
}