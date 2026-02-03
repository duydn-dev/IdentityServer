using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using IdentityServerHost.Data;

#nullable disable

namespace IdentityServerHost.Migrations.AuditDb
{
    [DbContext(typeof(AuditDbContext))]
    [Migration("20260203000000_InitialAuditDb")]
    partial class InitialAuditDb
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder.HasAnnotation("ProductVersion", "9.0.0");
            modelBuilder.HasAnnotation("Relational:MaxIdentifierLength", 63);
            NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

            modelBuilder.Entity("IdentityServerHost.Models.AuditLog", b =>
            {
                b.Property<long>("Id")
                    .ValueGeneratedOnAdd()
                    .HasColumnType("bigint");
                NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<long>("Id"));

                b.Property<string>("Action")
                    .IsRequired()
                    .HasMaxLength(200)
                    .HasColumnType("character varying(200)");

                b.Property<string>("Details")
                    .HasColumnType("text");

                b.Property<string>("EntityId")
                    .HasMaxLength(200)
                    .HasColumnType("character varying(200)");

                b.Property<string>("EntityType")
                    .HasMaxLength(100)
                    .HasColumnType("character varying(100)");

                b.Property<string>("IpAddress")
                    .HasMaxLength(50)
                    .HasColumnType("character varying(50)");

                b.Property<bool>("Success")
                    .HasColumnType("boolean");

                b.Property<DateTime>("Timestamp")
                    .HasColumnType("timestamp with time zone");

                b.Property<string>("UserAgent")
                    .HasMaxLength(500)
                    .HasColumnType("character varying(500)");

                b.Property<string>("UserId")
                    .HasColumnType("character varying(450)");

                b.Property<string>("UserName")
                    .HasColumnType("text");

                b.HasKey("Id");

                b.HasIndex("Action");

                b.HasIndex("Timestamp");

                b.HasIndex("UserId");

                b.ToTable("AuditLogs", (string)null);
            });
#pragma warning restore 612, 618
        }
    }
}
