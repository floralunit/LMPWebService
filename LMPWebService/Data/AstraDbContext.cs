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
            entity.ToTable("OuterMessageReader");

            entity.HasKey(e => e.OuterMessageReader_ID);

            entity.Property(e => e.OuterMessageReaderName)
                .IsRequired()
                .HasMaxLength(255);

            entity.Property(e => e.OuterSystem_ID)
                .IsRequired()
                .HasMaxLength(255);

            entity.Property(e => e.OuterMessageSourceName)
                .IsRequired()
                .HasMaxLength(255);

            entity.Property(e => e.InsDate)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.Property(e => e.LastSuccessReadDate)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.Property(e => e.InsApplicationUser_ID)
                .IsRequired()
                .HasMaxLength(255);

            entity.Property(e => e.UpdApplicationUser_ID)
                .IsRequired()
                .HasMaxLength(255);

            entity.Property(e => e.UpdDate)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        modelBuilder.Entity<OuterMessage>(entity =>
        {
            entity.ToTable("OuterMessage");

            entity.HasKey(e => e.OuterMessage_ID);
            entity.Property(e => e.OuterMessage_ID)
                .HasColumnType("uuid")
                .HasDefaultValueSql("gen_random_uuid()");

            entity.Property(e => e.OuterMessageReader_ID)
                .IsRequired()
                .HasMaxLength(255);

            entity.Property(e => e.MessageOuter_ID)
                .IsRequired()
                .HasMaxLength(255);

            entity.Property(e => e.ProcessingStatus)
                .IsRequired()
                .HasMaxLength(255);

            entity.Property(e => e.MessageText)
                .IsRequired()
                .HasMaxLength(255);

            entity.Property(e => e.ErrorMessage)
                .IsRequired()
                .HasMaxLength(255);
            entity.Property(e => e.InsDate)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.Property(e => e.UpdDate)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

        });
    }
}
