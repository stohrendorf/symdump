using frontend.Models;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace frontend.Migrations
{
    [DbContext(typeof(Context))]
    [UsedImplicitly]
    // ReSharper disable once PartialTypeWithSinglePart
    internal partial class ContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
            modelBuilder
                .HasAnnotation("ProductVersion", "1.1.2");

            modelBuilder.Entity(typeof(BinaryFile), b =>
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

            modelBuilder.Entity(typeof(Project), b =>
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

            modelBuilder.Entity(typeof(Project), b =>
            {
                b.HasOne(typeof(BinaryFile), "Exe")
                    .WithMany()
                    .HasForeignKey("ExeId");

                b.HasOne(typeof(BinaryFile), "Sym")
                    .WithMany()
                    .HasForeignKey("SymId");
            });
        }
    }
}