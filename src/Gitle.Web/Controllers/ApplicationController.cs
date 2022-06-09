namespace Gitle.Web.Controllers
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using Castle.MonoRail.Framework;
    using FluentNHibernate.Conventions;
    using Helpers;
    using Model;
    using Model.Helpers;
    using Model.Interfaces.Service;
    using Model.Nested;
    using NHibernate;
    using NHibernate.Linq;

    public class ApplicationController : SecureController
    {
        public ApplicationController(ISessionFactory sessionFactory) : base(sessionFactory)
        {
        }
        [Admin]
        public void Index(string customerSlug)
        {
            var applications = session.Query<Application>().Where(x => x.IsActive);
            if (!string.IsNullOrEmpty(customerSlug))
            {
                var customer = session.SlugOrDefault<Customer>(customerSlug);
                PropertyBag.Add("customer", customer);
                applications = applications.Where(x => x.Customer == customer);
            }
            PropertyBag.Add("items", applications.ToList());
        }

        [Admin]
        public void New(string customerSlug)
        {
            PropertyBag.Add("item", new Application());
            PropertyBag.Add("customers", session.Query<Customer>().Where(x => x.IsActive).OrderBy(x => x.Name).ToList());
            if (!string.IsNullOrEmpty(customerSlug))
            {
                PropertyBag.Add("customerId", session.Slug<Customer>(customerSlug));
            }
            RenderView("edit");
        }
        [Admin]
        public void Edit(string applicationSlug)
        {
            var application = session.SlugOrDefault<Application>(applicationSlug);
            PropertyBag.Add("item", application );
            PropertyBag.Add("customers", session.Query<Customer>().Where(x => x.IsActive).OrderBy(x => x.Name).ToList());
            PropertyBag.Add("customerId", application?.Customer?.Id);
        }

        [Admin]
        public void View(string applicationSlug)
        {
            var application = session.SlugOrDefault<Application>(applicationSlug);
            var installations = session.Query<Installation>().Where(x => x.Application == application && x.IsActive);
            if (!installations.IsEmpty())
            {
                PropertyBag.Add("installations", installations);
            }
            PropertyBag.Add("item", application);
        }
        [Admin]
        public void Save(string applicationSlug, long customerId)
        {
            var item = session.SlugOrDefault<Application>(applicationSlug);
            var customer = session.Query<Customer>().FirstOrDefault(x => x.IsActive && x.Id == customerId);
         

            if (item != null)
            {
                BindObjectInstance(item, "item");
            }
            else
            {
                item = BindObject<Application>("item");

            }

            item.Customer = customer;
            using (var tx = session.BeginTransaction())
            {
                session.SaveOrUpdate(item);
                tx.Commit();
            }

            RedirectToUrl("/application/index");
        }
        [Admin]
        public void Delete(string applicationSlug)
        {
            var application = session.SlugOrDefault<Application>(applicationSlug);
            application.Deactivate();
            using (var tx = session.BeginTransaction())
            {
                session.SaveOrUpdate(application);
                tx.Commit();
            }
            RedirectToReferrer();
        }

        [Admin]
        public void Comments(string applicationSlug, string comment)
        {
            var item = session.SlugOrDefault<Application>(applicationSlug);
            item.Comments = comment;

            using (var tx = session.BeginTransaction())
            {
                session.SaveOrUpdate(item);
                tx.Commit();
            }

            RenderText(comment);
        }

        [return: JSONReturnBinder]
        public object CheckApplicationName(string name, long applicationId)
        {
            var validName = !session.Query<Application>().Any(x => x.IsActive && x.Slug == name.Slugify() && x.Id != applicationId);
            var message = "Voer een naam in";
            if (!validName)
            {
                message = "Deze naam is al in gebruik, kies een andere";
            }
            return new { success = validName, message = message };
        }

    }
}
