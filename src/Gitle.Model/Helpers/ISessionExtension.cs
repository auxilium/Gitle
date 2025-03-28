namespace Gitle.Model.Helpers
{
    using System;
    using System.Linq;
    using Interfaces.Model;
    using NHibernate;
    using NHibernate.Linq;

    public static class ISessionExtension
    {
        public static T Slug<T>(this ISession session, string slug) where T : ISlugger
        {
            return session.Query<T>().Single(x => x.Slug == slug && x.IsActive);
        }

        public static T SlugOrDefault<T>(this ISession session, string slug) where T : ISlugger
        {
            return session.Query<T>().SingleOrDefault(x => x.Slug == slug && (x.IsActive || !x.IsActive));
        }

    }
}