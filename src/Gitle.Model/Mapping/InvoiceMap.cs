namespace Gitle.Model.Mapping
{
  using FluentNHibernate.Mapping;

  public class InvoiceMap : ModelBaseMap<Invoice>
  {
    public InvoiceMap()
    {
      Map(x => x.CreatedAt);
      Map(x => x.HourPrice);
      Map(x => x.Title);
      Map(x => x.Number);
      Map(x => x.VAT);
      Map(x => x.Remarks);
      Map(x => x.State);
      Map(x => x.StartDate);
      Map(x => x.EndDate);      

      References(x => x.Project).Column("Project_id");
      References(x => x.CreatedBy).Column("CreatedBy_id");

      HasMany(x => x.Lines).Cascade.AllDeleteOrphan();
      HasMany(x => x.Corrections).Cascade.AllDeleteOrphan();

      HasOne(x => x.Application);
    }
  }
}