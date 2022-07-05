namespace Gitle.Model.Mapping
{
    using FluentNHibernate.Mapping;

    public class LabelMap : ModelBaseMap<Label>
    {
        public LabelMap()
        {
            Map(x => x.Name);
            Map(x => x.Color);
            Map(x => x.VisibleForCustomer);
            Map(x => x.ApplicableByCustomer);
            Map(x => x.ToFreckle);
            Map(x => x.MakeIssueUrgent);
            References(x => x.Project).Column("Project_id").LazyLoad(Laziness.False);
        }
    }
}