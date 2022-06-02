using System.Configuration;

namespace Gitle.Web.Filters
{
    #region Usings

    using Castle.MonoRail.Framework;
    using System.Globalization;
    using System.Threading;

    #endregion

    public class LocalizationFilter : IFilter
    {
        public bool Perform(ExecuteWhen exec, IEngineContext context, IController controller,
                            IControllerContext controllerContext)
        {
            //IUser user = context.CurrentUser as IUser;
            //if (user != null)
            //{
            var configCulture = ConfigurationManager.AppSettings["culture"] ?? "nl-NL";
            Thread.CurrentThread.CurrentUICulture = CultureInfo.CreateSpecificCulture(configCulture);
            //NOTE: Door een bug in castle zijn we genoodzaakt de culture te zetten om de juiste resourcemanager te krijgen
            //http://issues.castleproject.org/issue/MR-573
            //Castle.MonoRail.Framework.Resources.DefaultResourceFactory.ResolveCulture(string), Castle.MonoRail.Framework
            Thread.CurrentThread.CurrentCulture = Thread.CurrentThread.CurrentUICulture;
            //}
            return true;
        }
    }
}