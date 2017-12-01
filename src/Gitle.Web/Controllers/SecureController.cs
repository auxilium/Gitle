namespace Gitle.Web.Controllers
{
    using System;
    using System.IO;
    using System.Text;
    using Castle.MonoRail.Framework;
    using Model;
    using Filters;
    using System.Collections.Generic;
    using System.Configuration;
    using System.Linq;
    using System.Reflection;
    using AntiCSRF;
    using Helpers;
    using NHibernate;

    [Layout("secure")]
    [Filter(ExecuteWhen.BeforeAction, typeof (AuthorisationFilter), ExecutionOrder = 1)]
    [Filter(ExecuteWhen.BeforeAction, typeof (AuthenticationFilter), ExecutionOrder = 2)]
    public abstract class SecureController : BaseController
    {
        protected SecureController(ISessionFactory sessionFactory) : base(sessionFactory)
        {
        }

        protected User CurrentUser => (User) Context.CurrentUser;

        protected override object InvokeMethod(MethodInfo method, IRequest request, IDictionary<string, object> extraArgs)
        {
            var adminAttributes = (AdminAttribute[])method.GetCustomAttributes(typeof(AdminAttribute), false);
            if (adminAttributes.Length > 0 && !CurrentUser.IsAdmin)
            {
                RenderView("/shared/forbidden");
                return null;
            }
            var bookHoursAttributes = (BookHoursAttribute[])method.GetCustomAttributes(typeof(BookHoursAttribute), false);
            if (bookHoursAttributes.Length > 0 && !CurrentUser.CanBookHours)
            {
                RenderView("/shared/forbidden");
                return null;
            }
            var danielleAttributes = (DanielleAttribute[])method.GetCustomAttributes(typeof(DanielleAttribute), false);
            if (danielleAttributes.Length > 0 && !CurrentUser.IsDanielle)
            {
                RenderView("/shared/forbidden");
                return null;
            }
            var projectAttributes = (MustHaveProjectAttribute[])method.GetCustomAttributes(typeof(MustHaveProjectAttribute), false);
            if (!CurrentUser.IsAdmin && extraArgs.ContainsKey("projectSlug") && projectAttributes.Length > 0 && CurrentUser.Projects.Select(p => p.Project).All(p => p.Slug != extraArgs["projectSlug"].ToString()))
            {
                RenderView("/shared/forbidden");
                return null;
            }

            return base.InvokeMethod(method, request, extraArgs);
        }
        
        protected void ServeCsv(string exportName, string csv)
        {
            CancelView();

            Response.ClearContent();
            Response.Clear();

            Response.AppendHeader("content-disposition", $"attachment; filename={$"export_{DateTime.Now:yyyyMMdd_hhmmss}.csv"}");
            Response.ContentType = "application/csv";

            var byteArray = Encoding.Default.GetBytes(csv);
            var stream = new MemoryStream(byteArray);
            Response.BinaryWrite(stream);
        }
    }
}