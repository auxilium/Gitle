namespace Gitle.Model.Mapping
{
    public class ApplicationMap : ModelBaseMap<Application>
    {
        public ApplicationMap ()
        {
            Map(x => x.Name);
            Map(x => x.RepositoryUrl);
            Map(x => x.Slug);
            Map(x => x.Comments).CustomSqlType("nvarchar(max)");
            References(x => x.Customer);
            HasMany<Project>(x => x.Projects).Cascade.DeleteOrphan();
        }
    }
}