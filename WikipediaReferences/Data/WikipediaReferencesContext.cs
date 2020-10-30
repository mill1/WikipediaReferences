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
        // TODO Models. (Attr)
        public virtual DbSet<Models.Attribute> Attributes { get; set; }
        public virtual DbSet<DataType> DataTypes { get; set; }

        public WikipediaReferencesContext(DbContextOptions<WikipediaReferencesContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Models.Attribute>(entity =>
            {
                entity.Property(e => e.DataTypeCode)
                    .IsRequired()
                    .HasMaxLength(35);

                entity.Property(e => e.Name)
                    .HasMaxLength(255);
            });

            modelBuilder.Entity<DataType>(entity =>
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
