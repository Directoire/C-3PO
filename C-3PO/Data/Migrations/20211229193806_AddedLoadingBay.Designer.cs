﻿// <auto-generated />
using System;
using C_3PO.Data.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace C_3PO.Data.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20211229193806_AddedLoadingBay")]
    partial class AddedLoadingBay
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "6.0.1")
                .HasAnnotation("Relational:MaxIdentifierLength", 64);

            modelBuilder.Entity("C_3PO.Data.Models.Category", b =>
                {
                    b.Property<ulong>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint unsigned");

                    b.Property<ulong?>("Feed")
                        .HasColumnType("bigint unsigned");

                    b.Property<string>("RSS")
                        .HasColumnType("longtext");

                    b.Property<ulong>("Role")
                        .HasColumnType("bigint unsigned");

                    b.HasKey("Id");

                    b.ToTable("Categories");
                });

            modelBuilder.Entity("C_3PO.Data.Models.Configuration", b =>
                {
                    b.Property<ulong>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint unsigned");

                    b.Property<ulong>("Civilian")
                        .HasColumnType("bigint unsigned");

                    b.Property<ulong>("Conduct")
                        .HasColumnType("bigint unsigned");

                    b.Property<ulong>("Ejected")
                        .HasColumnType("bigint unsigned");

                    b.Property<ulong>("Hangar")
                        .HasColumnType("bigint unsigned");

                    b.Property<ulong>("LoadingBay")
                        .HasColumnType("bigint unsigned");

                    b.Property<bool>("Lockdown")
                        .HasColumnType("tinyint(1)");

                    b.Property<ulong>("Logs")
                        .HasColumnType("bigint unsigned");

                    b.Property<ulong>("Onboarding")
                        .HasColumnType("bigint unsigned");

                    b.Property<ulong>("OuterRim")
                        .HasColumnType("bigint unsigned");

                    b.Property<ulong>("Rules")
                        .HasColumnType("bigint unsigned");

                    b.Property<ulong>("Unidentified")
                        .HasColumnType("bigint unsigned");

                    b.HasKey("Id");

                    b.ToTable("Configurations");
                });

            modelBuilder.Entity("C_3PO.Data.Models.Infraction", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<bool>("Active")
                        .HasColumnType("tinyint(1)");

                    b.Property<DateTime>("ExpiresOn")
                        .HasColumnType("datetime(6)");

                    b.Property<DateTime>("IssuedOn")
                        .HasColumnType("datetime(6)");

                    b.Property<ulong>("Moderator")
                        .HasColumnType("bigint unsigned");

                    b.Property<string>("Reason")
                        .HasColumnType("longtext");

                    b.Property<int>("Type")
                        .HasColumnType("int");

                    b.Property<ulong>("User")
                        .HasColumnType("bigint unsigned");

                    b.HasKey("Id");

                    b.ToTable("Infractions");
                });

            modelBuilder.Entity("C_3PO.Data.Models.NotificationRole", b =>
                {
                    b.Property<ulong>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint unsigned");

                    b.Property<ulong?>("CategoryId")
                        .HasColumnType("bigint unsigned");

                    b.HasKey("Id");

                    b.HasIndex("CategoryId")
                        .IsUnique();

                    b.ToTable("NotificationRoles");
                });

            modelBuilder.Entity("C_3PO.Data.Models.Onboarding", b =>
                {
                    b.Property<ulong>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint unsigned");

                    b.Property<ulong>("ActionMessage")
                        .HasColumnType("bigint unsigned");

                    b.Property<ulong>("CategoriesMessage")
                        .HasColumnType("bigint unsigned");

                    b.Property<ulong>("Channel")
                        .HasColumnType("bigint unsigned");

                    b.Property<ulong>("NotificationsMessage")
                        .HasColumnType("bigint unsigned");

                    b.Property<ulong>("OfferMessage")
                        .HasColumnType("bigint unsigned");

                    b.Property<ulong>("RulesMessage")
                        .HasColumnType("bigint unsigned");

                    b.Property<int>("State")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.ToTable("Onboardings");
                });

            modelBuilder.Entity("C_3PO.Data.Models.NotificationRole", b =>
                {
                    b.HasOne("C_3PO.Data.Models.Category", "Category")
                        .WithOne("NotificationRole")
                        .HasForeignKey("C_3PO.Data.Models.NotificationRole", "CategoryId");

                    b.Navigation("Category");
                });

            modelBuilder.Entity("C_3PO.Data.Models.Category", b =>
                {
                    b.Navigation("NotificationRole");
                });
#pragma warning restore 612, 618
        }
    }
}
