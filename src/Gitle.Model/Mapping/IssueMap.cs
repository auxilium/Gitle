namespace Gitle.Model.Mapping
{
    using FluentNHibernate.Mapping;

    public class IssueMap : TouchableMap<Issue>
    {
        public IssueMap()
        {
            Map(x => x.Number);
            Map(x => x.Name);
            Map(x => x.Body).CustomSqlType("nvarchar(max)").CustomType("StringClob");
            Map(x => x.Hours);
            Map(x => x.Devvers);
            Map(x => x.Prioritized);
            Map(x => x.PrioOrder);
            Map(x => x.EstimatePublic);
            Map(x => x.Administrative);
            Map(x => x.Urgent);
            References(x => x.Project).Column("Project_id").LazyLoad(Laziness.False);
            HasMany(x => x.Comments).Cascade.AllDeleteOrphan();
            HasMany(x => x.ChangeStates).Cascade.AllDeleteOrphan();
            HasMany(x => x.Changes).Cascade.AllDeleteOrphan();
            HasMany(x => x.Pickups).Cascade.AllDeleteOrphan();
            HasMany(x => x.HandOvers).Cascade.AllDeleteOrphan();
            HasMany(x => x.Bookings).Cascade.AllDeleteOrphan();
            HasManyToMany(x => x.Labels).Table("IssueLabel").ParentKeyColumn("Issue_id").ChildKeyColumn("Label_id").Cascade.SaveUpdate();
            HasMany(x => x.InvoiceLines).Cascade.None();
        }
    }
}