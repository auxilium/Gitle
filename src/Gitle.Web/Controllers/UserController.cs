namespace Gitle.Web.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using System.Data.SqlClient;
    using System.Linq;
    using System.Web.UI;
    using Castle.MonoRail.Framework;
    using FluentNHibernate.Utils;
    using Gitle.Model.James;
    using Helpers;
    using Model;
    using Model.Helpers;
    using Model.Nested;
    using NHibernate;
    using NHibernate.Context;
    using NHibernate.Linq;

    public class UserController : SecureController
    {
        public UserController(ISessionFactory sessionFactory) : base(sessionFactory)
        {
        }

        [Admin]
        public void Index()
        {
            var items = session.Query<User>().Where(user => user.IsActive).ToList();
            PropertyBag.Add("items", items);
        }

        [Admin]
        public void New()
        {
            PropertyBag.Add("item", new User());

            var jamesEmployees = new List<Employee>();
            var sqlConnectionHelper = new SqlConnectionHelper();

            using (var reader = sqlConnectionHelper.ExecuteSqlQuery("james", "SELECT Id, Voornaam, Achternaam FROM Medewerker WHERE Actief = 1"))
            {
                while (reader.Read())
                {
                    jamesEmployees.Add(new Employee
                    {
                        Id = (int)reader[0],
                        FirstName = reader[1].ToString(),
                        LastName = reader[2].ToString()
                    });
                }
            }

            sqlConnectionHelper.CloseSqlConnection();

            PropertyBag.Add("selectedprojects", new List<Project>());
            PropertyBag.Add("customers", session.Query<Customer>().Where(x => x.IsActive).ToList());
            PropertyBag.Add("notificationprojects", new List<Project>());
            PropertyBag.Add("projects", session.Query<Project>().Where(x => x.IsActive).OrderBy(x => x.Name).ToList());
            PropertyBag.Add("jamesEmployees", jamesEmployees.ToList());
            RenderView("edit");
        }

        [Admin]
        public void Edit(long userId, bool setCustomer, long? customerId)
        {
            if (!customerId.HasValue)
            {
                customerId = 0;
            }
            var user = session.Get<User>(userId);

            if (customerId == 0)
            {
                customerId = user.Customer?.Id;
            }

            var customer = session.Query<Customer>().Where(x => x.IsActive).ToList();
            List<Project> projects;
            if (user.Customer != null)
            {
                var application = session.Query<Application>().FirstOrDefault(a => a.Customer.Id == customerId) ?? throw new Exception("Error application");
                projects = session.Query<Project>()
                    .Where(p => p.Application.Id == application.Id)
                    .Where(x => x.IsActive)
                    .OrderBy(x => x.Name)
                    .ToList();
            }
            else
            {
                projects = session.Query<Project>()
                   .Where(x => x.IsActive)
                   .OrderBy(x => x.Name)
                   .ToList();
            }

            var jamesEmployees = new List<Employee>();
            var sqlConnectionHelper = new SqlConnectionHelper();

            using (var reader = sqlConnectionHelper.ExecuteSqlQuery("james", "SELECT Id, Voornaam, Achternaam FROM Medewerker WHERE Actief = 1"))
            {
                while (reader.Read())
                {
                    jamesEmployees.Add(new Employee
                    {
                        Id = (int)reader[0],
                        FirstName = reader[1].ToString(),
                        LastName = reader[2].ToString()
                    });
                }
            }

            sqlConnectionHelper.CloseSqlConnection();

            PropertyBag.Add("item", user);

            PropertyBag.Add("selectedprojects", user.Projects.Select(x => x.Project).ToList());
            PropertyBag.Add("customers", customer);
            PropertyBag.Add("projects", projects);
            PropertyBag.Add("customerId", user.Customer?.Id ?? 0);
            PropertyBag.Add("jamesEmployees", jamesEmployees.ToList());

            if (setCustomer)
            {
                var password = user.Password.ToString();
                if ((!string.IsNullOrWhiteSpace(password) && !password.Contains("Gitle.Model.Nested.Password")) || user.Password == null)
                {
                    user.Password = new Password(password);
                }
                Save(userId, password, customerId);
                RedirectToUrl($"/user/{user.Id}/edit");
            }

        }

        public void View(long userId)
        {
            var user = session.Get<User>(userId);
            PropertyBag.Add("item", user);
        }

        public void Profile()
        {
            PropertyBag.Add("item", CurrentUser);
            PropertyBag.Add("projects", CurrentUser.Projects.Select(up => up.Project).ToList());
            PropertyBag.Add("notificationprojects", CurrentUser.Projects.Where(up => up.Notifications).Select(up => up.Project).ToList());
            PropertyBag.Add("ownnotificationprojects", CurrentUser.Projects.Where(up => up.Notifications && up.OnlyOwnIssues).Select(up => up.Project).ToList());
        }

        public void SaveProfile(string password, int[] notificationprojects, int[] ownnotificationprojects, long[] filterpresets, long customerId)
        {
            var item = CurrentUser;
            if (item != null)
            {
                BindObjectInstance(item, "item");
            }
            else
            {
                item = BindObject<User>("item");
            }

            if (!string.IsNullOrWhiteSpace(password) || item.Password == null)
            {
                item.Password = new Password(password);
            }

            item.Customer = session.Get<Customer>(customerId);

            item.Projects.Each(up => up.Notifications = false);
            notificationprojects.Each(i => item.Projects.First(up => up.Project.Id == i).Notifications = true);

            item.Projects.Each(up => up.OnlyOwnIssues = false);
            ownnotificationprojects.Each(i => item.Projects.First(up => up.Project.Id == i).OnlyOwnIssues = true);

            var filterPresetsToDelete = item.FilterPresets.Where(l => !filterpresets.Contains(l.Id)).ToList();

            using (var tx = session.BeginTransaction())
            {
                foreach (var filterPreset in filterPresetsToDelete)
                {
                    item.FilterPresets.Remove(filterPreset);
                    session.Delete(filterPreset);
                }
                session.SaveOrUpdate(item);
                tx.Commit();
            }
            RedirectToSiteRoot();
        }

        [Admin]
        public void Delete(long userId)
        {
            var user = session.Get<User>(userId);
            user.Deactivate();
            using (var tx = session.BeginTransaction())
            {
                session.SaveOrUpdate(user);
                tx.Commit();
            }
            RedirectToReferrer();
        }

        [Admin]
        public void Save(long userId, string password, long? customerId)
        {
            var item = session.Get<User>(userId);
            if (item != null)
            {
                BindObjectInstance(item, "item");
            }
            else
            {
                item = BindObject<User>("item");
            }

            if ((!string.IsNullOrWhiteSpace(password) && !password.Contains("Gitle.Model.Nested.Password")) || item.Password == null)
            {
                item.Password = new Password(password);
            }

            item.Customer = session.Get<Customer>(customerId);

            var userProjects = BindObject<UserProject[]>("userProject");

            var subscriptions = userProjects.Where(x => x.Subscribed).ToList();

            var userProjectsToDelete = item.Projects.Where(project => subscriptions.All(subscription => subscription.Id != project.Id)).ToList();

            foreach (var subscription in subscriptions)
            {
                var userProject = item.Projects.FirstOrDefault(x => x.Id > 0 && x.Id == subscription.Id);
                if (userProject != null)
                {
                    userProject.Notifications = subscription.Notifications;
                    userProject.OnlyOwnIssues = subscription.OnlyOwnIssues;
                }
                else
                {
                    subscription.User = item;
                    item.Projects.Add(subscription);
                }
            }

            using (var tx = session.BeginTransaction())
            {
                session.SaveOrUpdate(item);

                foreach (var userProject in userProjectsToDelete)
                {
                    userProject.User = null;
                    item.Projects.Remove(userProject);
                }
                tx.Commit();
            }

            RedirectToUrl("/users");
        }

        [Admin]
        public void Comments(long userId, string comment)
        {
            var item = session.Get<User>(userId);

            item.Comments = comment;

            using (var tx = session.BeginTransaction())
            {
                session.SaveOrUpdate(item);
                tx.Commit();
            }

            RenderText(comment);
        }

        [return: JSONReturnBinder]
        public object CheckUserName(string name, long userId)
        {
            var validName = !session.Query<User>().Any(x => x.IsActive && x.Name == name && x.Id != userId);
            var message = "Voer een naam in";
            if (!validName)
            {
                message = "Deze naam is al in gebruik, kies een andere";
            }
            return new { success = validName, message = message };
        }

        [Admin]
        public void Issues(long userId)
        {
            var allProjects = session.Query<Project>().Where(x => x.IsActive && !x.Closed);
            var projects = new List<Project>();

            foreach (var project in allProjects)
            {
                foreach (var issue in project.Issues)
                {
                    if (issue.PickedUpBy?.Id == userId && !issue.IsClosed)
                        projects.Add(project);
                }
            }

            PropertyBag.Add("user", session.Get<User>(userId));
            PropertyBag.Add("projects", projects.Distinct());
            PropertyBag.Add("count", projects.Distinct().Count());
        }


    }
}