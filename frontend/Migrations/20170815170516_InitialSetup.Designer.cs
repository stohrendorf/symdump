using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using frontend.Models;

namespace frontend.Migrations
{
    [DbContext(typeof(Context))]
    [Migration("20170815170516_InitialSetup")]
    partial class InitialSetup
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
            modelBuilder
                .HasAnnotation("ProductVersion", "1.1.2");

            modelBuilder.Entity(typeof(frontend.Models.BinaryFile), b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<byte[]>("Data")
                        .IsRequired();

                    b.Property<string>("Name")
                        .IsRequired();

                    b.HasKey("Id");

                    b.ToTable("BinaryFile");
                });

            modelBuilder.Entity(typeof(frontend.Models.Project), b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<int?>("ExeId");

                    b.Property<int?>("SymId");

                    b.HasKey("Id");

                    b.HasIndex("ExeId");

                    b.HasIndex("SymId");

                    b.ToTable("Projects");
                });

            modelBuilder.Entity(typeof(frontend.Models.Project), b =>
                {
                    b.HasOne(typeof(frontend.Models.BinaryFile), "Exe")
                        .WithMany()
                        .HasForeignKey("ExeId");

                    b.HasOne(typeof(frontend.Models.BinaryFile), "Sym")
                        .WithMany()
                        .HasForeignKey("SymId");
                });
        }
    }
}
