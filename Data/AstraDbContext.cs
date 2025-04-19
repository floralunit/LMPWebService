using Microsoft.EntityFrameworkCore;
using LMPWebService.Models;

public class AstraDbContext : DbContext
{
    public AstraDbContext(DbContextOptions<AstraDbContext> options) : base(options) { }

    public DbSet<OuterMessageReader> OuterMessageReader { get; set; }
    public DbSet<OuterMessage> OuterMessage { get; set; }
    public DbSet<EMessage> EMessage { get; set; }
    public DbSet<FieldsToTrackForStatus_LMP> FieldsToTrackForStatus_LMP { get; set; }
    public DbSet<DictBase> DictBase { get; set; }
    public DbSet<DocumentBase> DocumentBase { get; set; }
    public DbSet<DocumentBaseParent> DocumentBaseParent { get; set; }
    public DbSet<WorkOrder> WorkOrder { get; set; }
    public DbSet<Contact> Contact { get; set; }
    public DbSet<Interest> Interest { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<OuterMessageReader>(entity =>
        {
            entity
                .ToTable("OuterMessageReader", "stella")
                .ToTable(tb => tb.HasTrigger("[stella].[TR_OuterMessageReader_AU_101]"))
                .HasKey(b => b.OuterMessageReader_ID);

        });

        modelBuilder.Entity<OuterMessage>(entity =>
        {
            entity
                .ToTable("OuterMessage", "stella")
                .ToTable(tb => tb.HasTrigger("[stella].[TR_OuterMessage_AU_101]"))
                .HasKey(b => b.OuterMessage_ID);

        });
        modelBuilder.Entity<EMessage>(entity =>
        {
            entity
                .ToTable("EMessage", "dbo")
                .HasKey(b => b.EMessage_ID);

        });
        modelBuilder.Entity<FieldsToTrackForStatus_LMP>(entity =>
        {
            entity
                .ToTable("FieldsToTrackForStatus_LMP", "stella")
                .HasKey(b => b.EMessage_ID);

        });
        modelBuilder.Entity<DictBase>(entity =>
        {
            entity
                .ToTable("DictBase", "dbo")
                .HasKey(b => b.DictBase_ID);

        });
        modelBuilder.Entity<DocumentBase>(entity =>
        {
            entity
                .ToTable("DocumentBase", "dbo")
                .HasKey(b => b.DocumentBase_ID);

        });
        modelBuilder.Entity<DocumentBaseParent>(entity =>
        {
            entity
                .ToTable("DocumentBaseParent", "dbo")
                .HasKey(b => b.DocumentBaseParent_ID);

        });
        modelBuilder.Entity<WorkOrder>(entity =>
        {
            entity
                .ToTable("WorkOrder", "dbo")
                .HasKey(b => b.WorkOrder_ID);

        });
        modelBuilder.Entity<Contact>(entity =>
        {
            entity
                .ToTable("Contact", "dbo")
                .HasKey(b => b.Contact_ID);

        });
        modelBuilder.Entity<Interest>(entity =>
        {
            entity
                .ToTable("Interest", "dbo")
                .HasKey(b => b.Interest_ID);

        });
    }
}
