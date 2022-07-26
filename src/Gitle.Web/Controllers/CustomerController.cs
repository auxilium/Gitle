namespace Gitle.Web.Controllers
{
    using System.Collections.Generic;
    using System.Linq;
    using Castle.MonoRail.Framework;
    using Clients.Freckle.Interfaces;
    using FluentNHibernate.Utils;
    using Model;
    using Helpers;
    using Model.Enum;
    using Model.Helpers;
    using NHibernate;
    using NHibernate.Linq;

    public class CustomerController : SecureController
    {
        public CustomerController(ISessionFactory sessionFactory) : base(sessionFactory)
        {
        }

        [Admin]
        public void Index()
        {
            PropertyBag.Add("items", session.Query<Customer>().Where(x => x.IsActive).ToList());
        }

        [Admin]
        public void View(string customerSlug)
        {
            var customer = session.SlugOrDefault<Customer>(customerSlug);
            var customerContacts = customer.Contacts.Where(x => x.IsActive).ToList();
            var applications = session.Query<Application>()
                .Where(x => x.Customer == customer && x.IsActive)
                .OrderBy(x => x.Name);
            var projects = session.Query<Project>().Where(a => a.Application == applications.FirstOrDefault() && a.IsActive);

            PropertyBag.Add("applications", applications);
            PropertyBag.Add("projects", projects);
            PropertyBag.Add("customer", customer);
            PropertyBag.Add("customerContacts", customerContacts);
        }

        [Admin]
        public void New()
        {
            PropertyBag.Add("item", new Customer());
            RenderView("edit");
        }

        [Admin]
        public void Edit(string customerSlug)
        {
            var customer = session.SlugOrDefault<Customer>(customerSlug);
            PropertyBag.Add("item", customer);
        }

        [Admin]
        public void Delete(string customerSlug)
        {
            var customer = session.SlugOrDefault<Customer>(customerSlug);
            customer.Deactivate();
            using (var tx = session.BeginTransaction())
            {
                session.SaveOrUpdate(customer);
                tx.Commit();
            }
            RedirectToReferrer();
        }

        [Admin]
        public void Save(string customerSlug)
        {
            var item = session.SlugOrDefault<Customer>(customerSlug);
            if (item != null)
            {
                BindObjectInstance(item, "item");
            }
            else
            {
                item = BindObject<Customer>("item");
            }

            using (var tx = session.BeginTransaction())
            {
                session.SaveOrUpdate(item);
                tx.Commit();
            }

            RedirectToUrl("/customers");
        }

        [Admin]
        public void Comments(string customerSlug, string comment)
        {
            var item = session.SlugOrDefault<Customer>(customerSlug);

            item.Comments = comment;

            using (var tx = session.BeginTransaction())
            {
                session.SaveOrUpdate(item);
                tx.Commit();
            }

            RenderText(comment);
        }

        [return: JSONReturnBinder]
        public object CheckCustomerName(string name, long customerId)
        {
            var validName = !session.Query<Customer>().Any(x => x.IsActive && x.Slug == name.Slugify() && x.Id != customerId);
            var message = "Voer een naam in";
            if (!validName)
            {
                message = "Deze naam is al in gebruik, kies een andere";
            }
            return new { success = validName, message = message };
        }
    }
}