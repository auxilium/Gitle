﻿namespace Gitle.Repository
{
    #region Usings

    using System.Collections.Generic;
    using System.Linq;
    using Model;
    using Model.Interfaces.Repository;
    using NHibernate;

    #endregion

    public class UserRepository : BaseRepository<User>, IUserRepository
    {
        private readonly ISession session;

        public UserRepository(ISessionFactory sessionFactory) : base(sessionFactory)
        {
            session = sessionFactory.GetCurrentSession();
        }

        public User FindByName(string name)
        {
            var users = session.QueryOver<User>().Where(x => x.IsActive).And(x => x.Name == name).List();
            return users.Count > 0 ? users.First() : new User.NullUser();
        }

        public User FindByFullName(string fullName)
        {
            var users = session.QueryOver<User>().Where(x => x.IsActive).And(x => x.FullName == fullName).List();
            return users.Count > 0 ? users.First() : new User.NullUser();
        }

        public IList<User> FindByEmail(string email)
        {
            return session.QueryOver<User>().Where(x => x.IsActive).And(x => x.EmailAddress == email).List();
        }

        public IList<User> FindByPasswordHash(string hash)
        {
            return session.QueryOver<User>().Where(x => x.IsActive).And(x => x.Password.Hash == hash).List();
        }

        public IList<User> FindByGithubUser(string githubUser)
        {
            return session.QueryOver<User>().Where(x => x.IsActive).And(x => x.GitHubUsername == githubUser).List();
        }
    }
}