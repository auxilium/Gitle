using Gitle.Localization;

namespace Gitle.Web.Controllers
{
    #region Usings

    using Castle.MonoRail.Framework;
    using Model;
    using Model.Helpers;
    using Model.Interfaces.Service;
    using Model.Nested;
    using NHibernate;
    using System.Collections;
    using System.Configuration;
    using System.Linq;
    using System.Web.Security;

    #endregion

    public class AuthenticationController : BaseController
    {
        private readonly IEmailService emailService;

        public AuthenticationController(ISessionFactory sessionFactory, IEmailService emailService) : base(sessionFactory)
        {
            this.emailService = emailService;
        }

        public void Index(string name, string password, bool persistent, string returnUrl)
        {
            PropertyBag.Add("githubClientId", ConfigurationManager.AppSettings["githubClientId"]);
            PropertyBag.Add("githubOAuthCallback", ConfigurationManager.AppSettings["githubOAuthCallback"]);
            PropertyBag.Add("githubScope", ConfigurationManager.AppSettings["githubScope"]);
            var state = HashHelper.GenerateHash();
            Session.Add("state", state);
            PropertyBag.Add("state", state);

            if (string.IsNullOrWhiteSpace(name) && string.IsNullOrWhiteSpace(password))
            {
                PropertyBag.Add("returnUrl", returnUrl);
                return;
            }

            FormsAuthentication.Initialize();

            var users = session.QueryOver<User>().Where(x => x.IsActive).And(x => x.Name == name).List();
            var user = users.Count > 0 ? users.First() : new User.NullUser();

            if (user is User.NullUser || string.IsNullOrEmpty(password) || !user.Password.Match(password))
            {
                Error(Language.Authenticate_Failed, true);
                return;
            }
            var configCulture = ConfigurationManager.AppSettings["culture"];
            if (configCulture == "en-GB")
            {
               Context.Response.CreateCookie("culture", configCulture);
            }

            if (user.IsCustomer)
            {
                Context.Response.CreateCookie("role", "isCustomer");
            }
            FormsAuthentication.SetAuthCookie(name, persistent);
            
            if (string.IsNullOrEmpty(returnUrl))
                RedirectToSiteRoot();
            else
                RedirectToUrl(returnUrl);
        }

        public void ForgotPassword()
        {

        }

        public void RequestReset(string email)
        {
            var users = session.Query<User>().Where(x => !string.IsNullOrWhiteSpace(email) && x.IsActive && x.EmailAddress == email).ToList();

            if (users.Any())
            {
                users.ForEach(
                    user =>
                    {
                        if (user.Password == null) user.Password = new Password();
                        user.Password.GenerateHash();

                        emailService.SendPasswordLink(user);
                        using (var tx = session.BeginTransaction())
                        {
                            session.SaveOrUpdate(user);
                            tx.Commit();
                        }
                    });
            }
            Information(Language.Password_Change_Sent, true);
        }

        public void ChangePassword(string hash)
        {
            var users = session.QueryOver<User>().Where(x => x.IsActive).And(x => x.Password.Hash == hash).List();

            if (users.Count > 0 && !string.IsNullOrEmpty(hash))
            {
                PropertyBag.Add("hash", hash);
            }
            else
            {
                PropertyBag.Add("error", Language.Password_Change_Error_Expired);
            }
        }

        public void SavePassword(string hash, string password, string passwordCheck)
        {
            // password1 omdat 'password' ervoor zorgt dat bij redirect naar inlogpagina, het wachtwoordveld al wordt ingevuld
            if (password != passwordCheck)
            {

                PropertyBag.Add("error", Language.Password_Change_Error_Mismatch);
                PropertyBag.Add("hash", hash);
                RenderView("changepassword");
            }

            // check if hash (customer) exists
            var users = session.QueryOver<User>().Where(x => x.IsActive).And(x => x.Password.Hash == hash).List();

            // if hash (customer) exists
            if (users.Count > 0)
            {
                users[0].Password = new Password(password);
                using (var tx = session.BeginTransaction())
                {
                    session.SaveOrUpdate(users[0]);
                    tx.Commit();
                }

                Flash.Add("message", Language.Password_Change_Success);
                RenderView("index");
                Index("", "", false, "");
            }
            else
            {
                RedirectToAction("ChangePassword", new Hashtable { { "hash", hash } });
            }

        }

        public void Signout()
        {
            FormsAuthentication.SignOut();
            RedirectToSiteRoot();
        }

    }
}