using Microsoft.EntityFrameworkCore;
using susalem.EasyDemo.Entities;
using susalem.EasyDemo.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
namespace susalem.EasyDemo.Repository
{
    internal class JccRepository : DbContext
    {
        private string strDb = $"Data Source = {Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SQLite\\sqlite.db")}";

        public JccRepository()
        {
      
         
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!string.IsNullOrWhiteSpace(strDb))
            {
                optionsBuilder.UseSqlite(strDb);
            }
       
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // 自动写入初始角色数据
            modelBuilder.Entity<RoleModel>().HasData(
                new RoleModel { RoleId = 1, RoleName = "Admin", Level = 1 }, // 根据你的实际字段修改
                new RoleModel { RoleId = 2, RoleName = "User", Level =  2 }
            );

            // 如果需要，也可以初始化一个管理员账号
            // modelBuilder.Entity<UserModel>().HasData( ... );
        }

        /// <summary>
        /// 报警
        /// </summary>
        //public DbSet<AlarmModel>? Alarms { get; set; }


        //public DbSet<AlarmModel>? Alarms { get; set; }

        public DbSet<UserModel>? Users { get; set; }

        public DbSet<RoleModel>? Roles { get; set; }

        public DbSet<ChemicalParaModel>? ChemicalParas { get; set; }

        public DbSet<CabinetInfoModel>? CabinetInfos { get; set; }

        public DbSet<HistoryModel>? Historys { get; set; }
    }
}
