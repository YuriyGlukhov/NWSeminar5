using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NWSeminar5.Models
{
    public class ChatContext : DbContext
    {
         public virtual DbSet<User> Users { get; set; }
         public virtual DbSet<Message> Messages { get; set; }
        public ChatContext() 
        {
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {

            optionsBuilder
                
                .UseLazyLoadingProxies()
                .UseNpgsql("Host=localhost;Username=postgres;Password=example;Database=ChatDb");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Message>(e =>
            {
                e.HasKey(x => x.Id).HasName("messages_pkey");
                e.ToTable("Messages");
                e.Property(x => x.Id).HasColumnName("id");
                e.Property(x => x.Text).HasColumnName("text");
                e.Property(x => x.FromUserId).HasColumnName("from_user_id");
                e.Property(x => x.ToUserId).HasColumnName("to_user_id");

                e.HasOne(d => d.FromUser).WithMany(p => p.FromMessages)
                  .HasForeignKey(d => d.FromUserId)
                  .HasConstraintName("message_from_user_id_fkey");

                e.HasOne(d => d.ToUser).WithMany(p => p.ToMessages)
                  .HasForeignKey(d => d.ToUserId)
                  .HasConstraintName("message_to_user_id_fkey");
            });

            modelBuilder.Entity<User>(e =>
            {
                e.HasKey(x => x.Id).HasName("user_pkey");
                e.ToTable("Users");
                e.Property(x => x.Id).HasColumnName("id");
                e.Property(x => x.Name).HasMaxLength(255).HasColumnName("name");



            });

            base.OnModelCreating(modelBuilder);
        }
        
    }
}
