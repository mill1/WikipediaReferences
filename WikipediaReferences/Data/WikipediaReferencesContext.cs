//WikipediaReferencesContext
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WikipediaReferences.Models;

namespace WikipediaReferences.Data
{
    public class WikipediaReferencesContext : DbContext
    {
        public virtual DbSet<Article> References { get; set; }
        public virtual DbSet<Source> Sources { get; set; }

        public WikipediaReferencesContext(DbContextOptions<WikipediaReferencesContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Article>(entity =>
            {
                entity.Property(e => e.type)
                    .IsRequired()
                    .HasMaxLength(35);

                entity.Property(e => e.SourceCode)
                    .IsRequired()
                    .HasMaxLength(35);
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
