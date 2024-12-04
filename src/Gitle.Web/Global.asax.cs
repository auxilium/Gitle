namespace Gitle.Web
{
  #region Usings

  using System;
  using System.Linq;
  using System.Net;
  using System.Reflection;
  using System.Web;
  using Castle.MicroKernel.Registration;
  using Castle.MonoRail.Framework;
  using Castle.MonoRail.Framework.Configuration;
  using Castle.MonoRail.Framework.Container;
  using Castle.MonoRail.Framework.Helpers.ValidationStrategy;
  using Castle.MonoRail.Framework.JSGeneration;
  using Castle.MonoRail.Framework.JSGeneration.jQuery;
  using Castle.MonoRail.Framework.Routing;
  using Castle.Windsor;
  using Controllers;
  using FluentNHibernate.Cfg;
  using FluentNHibernate.Cfg.Db;
  using Model;
  using Model.Interfaces.Service;
  using NHibernate;
  using NHibernate.Cfg;
  using NHibernate.Context;
  using NHibernate.Tool.hbm2ddl;
  using NHibernate.Validator.Cfg;
  using NHibernate.Validator.Engine;
  using NHibernate.Validator.Event;
  using AllDefinitions = NHibernate.Validator.Cfg.Loquacious.AllDefinitions;
  using Environment = NHibernate.Validator.Cfg.Environment;

  #endregion

  public static class RoutingEngineExtension
  {
    public static void Add<T>(this RoutingEngine engine)
        where T : class, IController
    {
      var name = typeof(T).Name.Replace("Controller", "");
      var controllerDetails =
          typeof(T).GetCustomAttributes(typeof(ControllerDetailsAttribute), true).FirstOrDefault() as
          ControllerDetailsAttribute;
      var area = "";
      if (controllerDetails != null)
      {
        area = controllerDetails.Area;
      }

      var patternIdRoute =
          new PatternRoute($"{$"/{area}".TrimEnd('/')}/{name}/<id>/<action>")
              .DefaultForController().Is<T>()
              .Restrict("id").ValidInteger
              .DefaultForAction().Is("index");

      var patternRoute =
          new PatternRoute($"{$"/{area}".TrimEnd('/')}/{name}/<action>")
              .DefaultForController().Is<T>()
              .DefaultForAction().Is("index");

      if (!string.IsNullOrEmpty(area))
      {
        patternIdRoute.DefaultForArea().Is(area);
        patternRoute.DefaultForArea().Is(area);
      }
      engine.Add(patternIdRoute);
      engine.Add(patternRoute);
    }
  }

  public class Global : HttpApplication, IContainerAccessor, IMonoRailContainerEvents, IMonoRailConfigurationEvents
  {
    private static IWindsorContainer container;

    #region IContainerAccessor Members

    public IWindsorContainer Container
    {
      get { return container; }
    }

    #endregion

    #region IMonoRailConfigurationEvents Members

    public void Configure(IMonoRailConfiguration configuration)
    {
      configuration.JSGeneratorConfiguration.AddLibrary("jquery-1.8.2", typeof(JQueryGenerator)).AddExtension(
          typeof(CommonJSExtension)).ElementGenerator.AddExtension(typeof(JQueryElementGenerator)).Done.
          BrowserValidatorIs(typeof(JQueryValidator)).SetAsDefault();
    }

    #endregion

    #region IMonoRailContainerEvents Members

    public void Created(IMonoRailContainer container)
    {
    }

    public void Initialized(IMonoRailContainer container)
    {
    }

    #endregion

    protected void Application_Start(object sender, EventArgs e)
    {
      ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

      container = new GitleContainer();
      RegisterSessionFactory(container);

      RoutingModuleEx.Engine.Add(new PatternRoute("/")
                                     .DefaultForController().Is<ProjectController>()
                                     .DefaultForAction().Is("index"));

      RoutingModuleEx.Engine.Add(new PatternRoute("/signin")
                                     .DefaultForController().Is<AuthenticationController>()
                                     .DefaultForAction().Is("index"));
      RoutingModuleEx.Engine.Add(new PatternRoute("/signin/<action>")
                                     .DefaultForController().Is<AuthenticationController>());
      RoutingModuleEx.Engine.Add(new PatternRoute("/signin/changepassword/<hash>")
                                     .DefaultForController().Is<AuthenticationController>()
                                     .DefaultForAction().Is("changepassword"));
      RoutingModuleEx.Engine.Add(new PatternRoute("/signout")
                                     .DefaultForController().Is<AuthenticationController>()
                                     .DefaultForAction().Is("signout"));

      RoutingModuleEx.Engine.Add(new PatternRoute("/applications")
                                     .DefaultForController().Is<ApplicationController>()
                                     .DefaultForAction().Is("index"));
      RoutingModuleEx.Engine.Add(new PatternRoute("/application/<action>")
                                     .DefaultForController().Is<ApplicationController>()
                                     .DefaultForAction().Is("index"));
      RoutingModuleEx.Engine.Add(new PatternRoute("/application/<applicationSlug>/projects")
                                     .DefaultForController().Is<ProjectController>()
                                     .DefaultForAction().Is("index"));
      RoutingModuleEx.Engine.Add(new PatternRoute("/application/<applicationSlug>/newproject")
                                     .DefaultForController().Is<ProjectController>()
                                     .DefaultForAction().Is("new"));
      RoutingModuleEx.Engine.Add(new PatternRoute("/application/<applicationSlug>/<action>")
                                     .DefaultForController().Is<ApplicationController>()
                                     .DefaultForAction().Is("index"));

      RoutingModuleEx.Engine.Add(new PatternRoute("/users")
                                     .DefaultForController().Is<UserController>()
                                     .DefaultForAction().Is("index"));
      RoutingModuleEx.Engine.Add(new PatternRoute("/user/<action>")
                                     .DefaultForController().Is<UserController>()
                                     .DefaultForAction().Is("index"));
      RoutingModuleEx.Engine.Add(new PatternRoute("/user/<userId>/<action>")
                                     .DefaultForController().Is<UserController>()
                                     .DefaultForAction().Is("index"));

      RoutingModuleEx.Engine.Add(new PatternRoute("/customers")
                                     .DefaultForController().Is<CustomerController>()
                                     .DefaultForAction().Is("index"));
      RoutingModuleEx.Engine.Add(new PatternRoute("/customer/<action>")
                                     .DefaultForController().Is<CustomerController>()
                                     .DefaultForAction().Is("index"));
      RoutingModuleEx.Engine.Add(new PatternRoute("/customer/<customerSlug>/applications")
                                     .DefaultForController().Is<ApplicationController>()
                                     .DefaultForAction().Is("index"));
      RoutingModuleEx.Engine.Add(new PatternRoute("/customer/<customerSlug>/newapplication")
                                     .DefaultForController().Is<ApplicationController>()
                                     .DefaultForAction().Is("new"));
      RoutingModuleEx.Engine.Add(new PatternRoute("/customer/<customerSlug>/projects")
                                     .DefaultForController().Is<ProjectController>()
                                     .DefaultForAction().Is("index"));
      RoutingModuleEx.Engine.Add(new PatternRoute("/customer/<customerSlug>/newproject")
                                     .DefaultForController().Is<ProjectController>()
                                     .DefaultForAction().Is("new"));
      RoutingModuleEx.Engine.Add(new PatternRoute("/customer/<customerSlug>/<action>")
                                     .DefaultForController().Is<CustomerController>()
                                     .DefaultForAction().Is("index"));

      RoutingModuleEx.Engine.Add(new PatternRoute("/projects")
                                     .DefaultForController().Is<ProjectController>()
                                     .DefaultForAction().Is("index"));
      RoutingModuleEx.Engine.Add(new PatternRoute("/project/<action>")
                                     .DefaultForController().Is<ProjectController>()
                                     .DefaultForAction().Is("index"));
      RoutingModuleEx.Engine.Add(new PatternRoute("/project/<projectSlug>/<action>")
                                     .DefaultForController().Is<ProjectController>()
                                     .DefaultForAction().Is("index"));
      RoutingModuleEx.Engine.Add(new PatternRoute("/project/<projectSlug>/<action>")
                                     .DefaultForController().Is<ProjectController>()
                                     .DefaultForAction().Is("index"));
      RoutingModuleEx.Engine.Add(new PatternRoute("/project/<projectSlug>/<action>/<id>")
                                     .DefaultForController().Is<ProjectController>()
                                     .DefaultForAction().Is("index"));
      RoutingModuleEx.Engine.Add(new PatternRoute("issues", "/project/<projectSlug>/issues")
                                     .DefaultForController().Is<IssueController>()
                                     .DefaultForAction().Is("index"));
      RoutingModuleEx.Engine.Add(new PatternRoute("/project/<projectSlug>/issue/<action>")
                                     .DefaultForController().Is<IssueController>()
                                     .DefaultForAction().Is("index"));
      RoutingModuleEx.Engine.Add(new PatternRoute("/project/<projectSlug>/issue/<issueId>/<action>")
                                     .DefaultForController().Is<IssueController>()
                                     .DefaultForAction().Is("index"));
      RoutingModuleEx.Engine.Add(new PatternRoute("/project/<projectSlug>/issue/<issueId>/<action>/<param>")
                                     .DefaultForController().Is<IssueController>()
                                     .DefaultForAction().Is("index"));
      RoutingModuleEx.Engine.Add(new PatternRoute("/invoices")
                                     .DefaultForController().Is<InvoiceController>()
                                     .DefaultForAction().Is("index"));
      RoutingModuleEx.Engine.Add(new PatternRoute("/project/<projectSlug>/invoice/<action>")
                                     .DefaultForController().Is<InvoiceController>()
                                     .DefaultForAction().Is("index"));
      RoutingModuleEx.Engine.Add(new PatternRoute("/project/<projectSlug>/invoice/<invoiceId>/<action>")
                                     .DefaultForController().Is<InvoiceController>()
                                     .DefaultForAction().Is("index"));

      RoutingModuleEx.Engine.Add(new PatternRoute("/installation")
                                      .DefaultForController().Is<InstallationController>()
                                      .DefaultForAction().Is("index"));
      RoutingModuleEx.Engine.Add(new PatternRoute("/installation/<action>")
                                      .DefaultForController().Is<InstallationController>()
                                      .DefaultForAction().Is("index"));
      RoutingModuleEx.Engine.Add(new PatternRoute("/installation/<installationSlug>/<action>")
                                      .DefaultForController().Is<InstallationController>()
                                      .DefaultForAction().Is("index"));

      RoutingModuleEx.Engine.Add(new PatternRoute("/server")
                                      .DefaultForController().Is<ServerController>()
                                      .DefaultForAction().Is("index"));
      RoutingModuleEx.Engine.Add(new PatternRoute("/server/<action>")
                                      .DefaultForController().Is<ServerController>()
                                      .DefaultForAction().Is("index"));
      RoutingModuleEx.Engine.Add(new PatternRoute("/server/<serverSlug>/<action>")
                                      .DefaultForController().Is<ServerController>()
                                      .DefaultForAction().Is("index"));

      RoutingModuleEx.Engine.Add(new PatternRoute("/certificateinfo")
                                    .DefaultForController().Is<CertificateInfoController>()
                                    .DefaultForAction().Is("index"));
      RoutingModuleEx.Engine.Add(new PatternRoute("/certificateinfo/<action>")
                                      .DefaultForController().Is<CertificateInfoController>()
                                      .DefaultForAction().Is("index"));
      RoutingModuleEx.Engine.Add(new PatternRoute("/certificateinfo/<certificateInfoSlug>/<action>")
                                      .DefaultForController().Is<CertificateInfoController>()
                                      .DefaultForAction().Is("index"));


      RoutingModuleEx.Engine.Add(new PatternRoute("/hosting")
                .DefaultForController().Is<HostingController>()
                .DefaultForAction().Is("index"));
      RoutingModuleEx.Engine.Add(new PatternRoute("/hosting/<action>")
          .DefaultForController().Is<HostingController>()
          .DefaultForAction().Is("index"));
      RoutingModuleEx.Engine.Add(new PatternRoute("/hosting/<slug>/<action>")
          .DefaultForController().Is<HostingController>()
          .DefaultForAction().Is("index"));

      RoutingModuleEx.Engine.Add(new PatternRoute("/planning/<slug>/<action>")
                                     .DefaultForController().Is<PlanningController>()
                                     .DefaultForAction().Is("index"));

      RoutingModuleEx.Engine.Add(new PatternRoute("/search")
                                     .DefaultForController().Is<SearchController>()
                                     .DefaultForAction().Is("index"));
      RoutingModuleEx.Engine.Add(new PatternRoute("/search/<query>")
                                     .DefaultForController().Is<SearchController>()
                                     .DefaultForAction().Is("index"));

      RoutingModuleEx.Engine.Add<FilterPresetController>();
      RoutingModuleEx.Engine.Add<PlanningController>();
      RoutingModuleEx.Engine.Add<ReportPresetController>();
      RoutingModuleEx.Engine.Add<ImportController>();
      RoutingModuleEx.Engine.Add<IssueController>();
      RoutingModuleEx.Engine.Add<UploadController>();
      RoutingModuleEx.Engine.Add<SlugController>();
      RoutingModuleEx.Engine.Add<DatabaseController>();
      RoutingModuleEx.Engine.Add<OAuthController>();
      RoutingModuleEx.Engine.Add<BookingController>();
      RoutingModuleEx.Engine.Add<InvoiceController>();
      RoutingModuleEx.Engine.Add<ReportController>();
      RoutingModuleEx.Engine.Add<DashboardController>();
      RoutingModuleEx.Engine.Add<CertificateInfoController>();
    }

    protected void Application_End(object sender, EventArgs e)
    {
      container?.Dispose();
    }

    protected void Application_BeginRequest(object sender, EventArgs e)
    {
      var sessionFactory = Container.Resolve<ISessionFactory>();
      var session = sessionFactory.OpenSession();
      WebSessionContext.Bind(session);
    }

    protected void Application_EndRequest(object sender, EventArgs e)
    {
      var sessionFactory = Container.Resolve<ISessionFactory>();
      var session = WebSessionContext.Unbind(sessionFactory);
      if (session != null && session.IsOpen) session.Close();

      Response.Headers.Add("Content-Security-Policy", "frame-ancestors 'self'");
      Response.Headers.Add("X-Frame-Options", "SAMEORIGIN");
      Response.Headers.Remove("X-AspNet-Version");
    }

    protected static void RegisterSessionFactory(IWindsorContainer windsorContainer)
    {
      var configuration = Fluently.Configure()
          .Database(MsSqlConfiguration.MsSql2008.ConnectionString(c => c.FromConnectionStringWithKey("Gitle")))
          .ExposeConfiguration(c => c.CurrentSessionContext<WebSessionContext>())
          .ExposeConfiguration(c => ConfigureValidatorEngine<ModelBase>(c))
          .ExposeConfiguration(c => c.SetInterceptor(new GitleInterceptor(windsorContainer)))
          .ExposeConfiguration(ExportSchema)
          .Mappings(m => m.FluentMappings.AddFromAssemblyOf<ModelBase>());

      var sessionFactory = configuration.BuildSessionFactory();
      windsorContainer.Register(Component.For<ISessionFactory>().Instance(sessionFactory));
    }

    private static void ExportSchema(Configuration configuration)
    {
      new SchemaUpdate(configuration).Execute(false, true);
      //var scriptGenerator = new SchemaExport(configuration);
      //scriptGenerator.SetOutputFile(@"C:\AppOnline.sql");
      //scriptGenerator.Execute(true, false, false);
    }

    private static ValidatorEngine ConfigureValidatorEngine<T>(Configuration configuration)
    {
      Environment.SharedEngineProvider = new NHibernateSharedEngineProvider();
      var validatorEngine = Environment.SharedEngineProvider.GetEngine();
      var validatorConfiguration = new NHibernate.Validator.Cfg.Loquacious.FluentConfiguration();
      validatorConfiguration.SetDefaultValidatorMode(ValidatorMode.UseAttribute)
          .SetCustomResourceManager("Gitle.Localization.Language", Assembly.Load("Gitle.Localization"))
          .Register(AllDefinitions.ValidationDefinitions(typeof(T).Assembly))
          .IntegrateWithNHibernate
          .ApplyingDDLConstraints()
          .And
          .RegisteringListeners();
      validatorEngine.Configure(validatorConfiguration);

      configuration.Initialize(validatorEngine);
      return validatorEngine;
    }
  }
}