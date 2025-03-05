using Microsoft.EntityFrameworkCore;
using LMPWebService.Models;


public class AstraDbContext : DbContext
{
    public AstraDbContext(DbContextOptions<AstraDbContext> options) : base(options) { }

    public DbSet<OuterMessageReader> OuterMessageReader { get; set; }
    public DbSet<OuterMessage> OuterMessage { get; set; }

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
    }
}
