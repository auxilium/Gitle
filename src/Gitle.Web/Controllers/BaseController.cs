namespace Gitle.Web.Controllers
{
    #region Usings

    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using System.Linq;
    using System.Reflection;
    using AntiCSRF;
    using Castle.MonoRail.Framework;
    using Castle.MonoRail.Framework.Filters;
    using Helpers;
    using Model;
    using NHibernate;

    #endregion

    [Resource("t", "Gitle.Localization.Language", AssemblyName = "Gitle.Localization")]
    [Layout("default")]
    [Filter(ExecuteWhen.BeforeAction, typeof(RequestValidatorFilter), ExecutionOrder = int.MinValue)]
    [Filter(ExecuteWhen.BeforeAction, typeof(Filters.LocalizationFilter), ExecutionOrder = 1)]
    public abstract class BaseController : SmartDispatcherController
    {
        protected ISession session;

        protected BaseController(ISessionFactory sessionFactory)
        {
            session = sessionFactory.GetCurrentSession();
        }

        protected void Error(string message, bool redirectToReferrer = false)
        {
            CreateMessage("error", message, redirectToReferrer);
        }

        protected void Information(string message, bool redirectToReferrer = false)
        {
            CreateMessage("info", message, redirectToReferrer);
        }

        protected void Success(string message, bool redirectToReferrer = false)
        {
            CreateMessage("success", message, redirectToReferrer);
        }

        private void CreateMessage(string type, string message, bool redirectToReferrer)
        {
            Flash[type] = message;
            if(redirectToReferrer) 
                RedirectToReferrer();
        }

        protected override object InvokeMethod(MethodInfo method, IRequest request, IDictionary<string, object> extraArgs)
        {
            var release = true;
#if DEBUG
            release = false;
#endif
            PropertyBag.Add("RELEASE", release);

            PropertyBag.Add("provider", ConfigurationManager.AppSettings["provider"]);
            PropertyBag.Add("provider_phone", ConfigurationManager.AppSettings["provider_phone"]);

 
            if (Convert.ToBoolean(request.Params["cancelLayout"]))
            {
                CancelLayout();
            }

            var username = (Context.CurrentUser as User)?.Name ?? "nouser";

            var csrfAttributes = (CsrfAttribute[])method.GetCustomAttributes(typeof(CsrfAttribute), false);
            var csrfToken = ConfigurationManager.AppSettings["csrfToken"];
            if (csrfAttributes.Length > 0)
            {
                if (!request.Params.AllKeys.Contains("_csrfToken") || !AntiCSRFToken.ValidateToken(request.Params["_csrfToken"], csrfToken, username))
                {
                    throw new Exception("CSRF token not valid");
                }
            }

            PropertyBag["csrfToken"] = AntiCSRFToken.GenerateToken(username, csrfToken);

            return base.InvokeMethod(method, request, extraArgs);
        }
    }
}