namespace Gitle.Model.Mapping
{
    public class ProjectMap : ModelBaseMap<Project>
    {
        public ProjectMap()
        {
            Map(x => x.Number);
            Map(x => x.Name);
            Map(x => x.Slug);
            Map(x => x.Repository);
            Map(x => x.MilestoneId);
            Map(x => x.MilestoneName);
            Map(x => x.HourPrice);
            Map(x => x.FreckleId);
            Map(x => x.FreckleName);
            Map(x => x.Information).CustomSqlType("nvarchar(max)");
            Map(x => x.Comments).CustomSqlType("nvarchar(max)");
            Map(x => x.Type);
            Map(x => x.TicketRequiredForBooking);
            Map(x => x.BudgetMinutes);
            Map(x => x.Unbillable);
            Map(x => x.Closed);
            Map(x => x.IsMuted);
            HasMany(x => x.Users).Cascade.All();
            HasMany(x => x.Labels).Cascade.AllDeleteOrphan();
            HasMany(x => x.Issues).Cascade.None();
            HasMany(x => x.Invoices).Cascade.None();
            HasMany(x => x.Bookings).Cascade.None();
            HasManyToMany(x => x.Documents).Table("ProjectDocument").Cascade.AllDeleteOrphan();
            References(x => x.Application);
        }
    }
}