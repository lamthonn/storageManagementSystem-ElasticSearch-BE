using Microsoft.EntityFrameworkCore;
using DATN.Domain.Entities;
using System;

namespace DATN.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {

    }

    public DbSet<danh_muc> danh_muc { get; set; }
    public DbSet<nguoi_dung> nguoi_dung { get; set; }
    public DbSet<nhom_nguoi_dung> nhom_nguoi_dung { get; set; }
    public DbSet<dieu_huong> dieu_huong { get; set; }
    public DbSet<dm_command> dm_command { get; set; }
    public DbSet<nhat_ky_he_thong> nhat_ky_he_thong { get; set; }
    public DbSet<nguoi_dung_2_danh_muc> nguoi_dung_2_danh_muc { get; set; }
    public DbSet<nguoi_dung_2_nhom_nguoi_dung> nguoi_dung_2_nhom_nguoi_dung { get; set; }
    public DbSet<dieu_huong_2_command> dieu_huong_2_command { get; set; }
    public DbSet<nhom_nguoi_dung_2_command> nhom_nguoi_dung_2_command { get; set; }
    public DbSet<nhom_nguoi_dung_2_dieu_huong> nhom_nguoi_dung_2_dieu_huong { get; set; }
    public DbSet<tai_lieu> tai_lieu { get; set; }
    public DbSet<tai_lieu_2_nguoi_dung> tai_lieu_2_nguoi_dung { get; set; }


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        // Add your model configurations here

        modelBuilder.Entity<dieu_huong_2_command>()
        .ToTable("dieu_huong_2_command");
    }
}
