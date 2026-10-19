namespace Gitle.Model.Mapping
{
    using FluentNHibernate.Mapping;

    public class BookingMap : ModelBaseMap<Booking>
    {
         public BookingMap()
         {
             Map(x => x.CreatedAt);
             Map(x => x.Date);
             Map(x => x.Comment);
             Map(x => x.Minutes);
             Map(x => x.Unbillable);
             Map(x => x.Subsidized);

             References(x => x.Project).Column("Project_id");
             References(x => x.User).Column("User_id");
             References(x => x.Issue).Column("Issue_id");

             HasManyToMany(x => x.InvoiceLines).Table("InvoiceLineBooking").Cascade.None().Inverse();
         }
    }
}