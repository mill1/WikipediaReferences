using Microsoft.EntityFrameworkCore;
using WikipediaReferences.Models;

namespace WikipediaReferences.Data
{
    public class WRContext : DbContext
    {
        public virtual DbSet<Reference> References { get; set; }
        public virtual DbSet<Source> Sources { get; set; }

        public WRContext(DbContextOptions<WRContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Reference>(entity =>
            {
                entity.Property(e => e.Type)
                    .IsRequired()
                    .HasMaxLength(35);

                entity.Property(e => e.SourceCode)
                    .IsRequired()
                    .HasMaxLength(35);

                entity.Property(e => e.ArticleTitle)
                    .IsRequired()
                    .HasMaxLength(255);

                entity.Property(e => e.AccessDate).HasColumnType("date");
                entity.Property(e => e.Date).HasColumnType("date");
                entity.Property(e => e.DeathDate).HasColumnType("date");
                entity.Property(e => e.ArchiveDate).HasColumnType("date");
            });

            modelBuilder.Entity<Source>(entity =>
            {
                entity.HasKey(e => e.Code);

                entity.Property(e => e.Code)
                    .HasMaxLength(35);

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(255);
            });
        }
    }
}
