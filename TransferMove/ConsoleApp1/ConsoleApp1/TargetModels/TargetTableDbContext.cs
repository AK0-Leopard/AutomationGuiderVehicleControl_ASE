using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

#nullable disable

namespace ConsoleApp1.TargetModels
{
    public partial class TargetTableDbContext : DbContext
    {
        public TargetTableDbContext()
        {
        }

        public TargetTableDbContext(DbContextOptions<TargetTableDbContext> options)
            : base(options)
        {
        }

        public virtual DbSet<HTRANSFER> HTRANSFERs { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see http://go.microsoft.com/fwlink/?LinkId=723263.
                optionsBuilder.UseSqlServer(Program.TargetTableConnectionString);
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<HTRANSFER>(entity =>
            {
                entity.HasKey(e => new { e.ID, e.CMD_INSER_TIME })
                    .HasName("PK_HCMD_MCS");

                entity.Property(e => e.ID)
                    .IsUnicode(false)
                    .IsFixedLength(true);

                entity.Property(e => e.CARRIER_ID)
                    .IsUnicode(false)
                    .IsFixedLength(true);

                entity.Property(e => e.CHECKCODE)
                    .IsUnicode(false)
                    .IsFixedLength(true);

                entity.Property(e => e.EXCUTE_CMD_ID)
                    .IsUnicode(false)
                    .IsFixedLength(true);

                entity.Property(e => e.HOSTDESTINATION)
                    .IsUnicode(false)
                    .IsFixedLength(true);

                entity.Property(e => e.HOSTSOURCE)
                    .IsUnicode(false)
                    .IsFixedLength(true);

                entity.Property(e => e.ID_TIME)
                    .IsUnicode(false)
                    .IsFixedLength(true);

                entity.Property(e => e.LOT_ID)
                    .IsUnicode(false)
                    .IsFixedLength(true);

                entity.Property(e => e.PAUSEFLAG)
                    .IsUnicode(false)
                    .IsFixedLength(true);

                entity.Property(e => e.RESULT_CODE)
                    .IsUnicode(false)
                    .IsFixedLength(true);
            });

            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}
