﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Perikato.Data;

#nullable disable

namespace Perikato.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20240209111759_DeliveryRequestMatchesDELETED")]
    partial class DeliveryRequestMatchesDELETED
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "6.0.0")
                .HasAnnotation("Relational:MaxIdentifierLength", 128);

            SqlServerModelBuilderExtensions.UseIdentityColumns(modelBuilder, 1L, 1);

            modelBuilder.Entity("Perikato.Data.Carriers.MatchedDealIds", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<Guid>("MatchedDealId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<Guid>("RouteId")
                        .HasColumnType("uniqueidentifier");

                    b.HasKey("Id");

                    b.HasIndex("RouteId");

                    b.ToTable("MatchedDealIds");
                });

            modelBuilder.Entity("Perikato.Data.Carriers.RouteDates", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTime>("RouteDateTime")
                        .HasColumnType("datetime2");

                    b.Property<Guid>("RouteId")
                        .HasColumnType("uniqueidentifier");

                    b.HasKey("Id");

                    b.HasIndex("RouteId");

                    b.ToTable("RouteDates");
                });

            modelBuilder.Entity("Perikato.Data.Carriers.Routes", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTime?>("CreatedDate")
                        .HasColumnType("datetime2");

                    b.Property<float>("EndLatitude")
                        .HasColumnType("real");

                    b.Property<float>("EndLongitude")
                        .HasColumnType("real");

                    b.Property<bool>("IsActive")
                        .HasColumnType("bit");

                    b.Property<DateTime?>("LastModifiedDate")
                        .HasColumnType("datetime2");

                    b.Property<int>("Range")
                        .HasColumnType("int");

                    b.Property<float>("StartLatitude")
                        .HasColumnType("real");

                    b.Property<float>("StartLongitude")
                        .HasColumnType("real");

                    b.Property<string>("Status")
                        .HasColumnType("nvarchar(max)");

                    b.Property<Guid>("UserId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("UserName")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Vehicle")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Id");

                    b.HasIndex("UserId");

                    b.ToTable("Routes");
                });

            modelBuilder.Entity("Perikato.Data.Dealers.DeliveryRequest", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTime>("CreatedDate")
                        .HasColumnType("datetime2");

                    b.Property<string>("CustomerNotes")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("DeliveryAddress")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Description")
                        .HasColumnType("nvarchar(max)");

                    b.Property<float>("EndLatitude")
                        .HasColumnType("real");

                    b.Property<float>("EndLongitude")
                        .HasColumnType("real");

                    b.Property<bool>("IsActive")
                        .HasColumnType("bit");

                    b.Property<DateTime>("LastModifiedDate")
                        .HasColumnType("datetime2");

                    b.Property<string>("PickUpAddress")
                        .HasColumnType("nvarchar(max)");

                    b.Property<float?>("Price")
                        .HasColumnType("real");

                    b.Property<float>("StartLatitude")
                        .HasColumnType("real");

                    b.Property<float>("StartLongitude")
                        .HasColumnType("real");

                    b.Property<string>("Status")
                        .HasColumnType("nvarchar(max)");

                    b.Property<Guid>("UserId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("UserName")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("VehicleRecommendation")
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Id");

                    b.ToTable("DeliveryRequest");
                });

            modelBuilder.Entity("Perikato.Data.Dealers.Package", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<Guid>("DeliveryRequestId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("Size")
                        .HasColumnType("nvarchar(max)");

                    b.Property<float?>("Weight")
                        .HasColumnType("real");

                    b.HasKey("Id");

                    b.HasIndex("DeliveryRequestId");

                    b.ToTable("Packages");
                });

            modelBuilder.Entity("Perikato.Data.Dealers.PreferredPickUpDates", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<Guid>("DeliveryRequestId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTime?>("PickUpDate")
                        .HasColumnType("datetime2");

                    b.HasKey("Id");

                    b.HasIndex("DeliveryRequestId");

                    b.ToTable("preferredPickUpDates");
                });

            modelBuilder.Entity("Perikato.Data.Dealers.TimeRange", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<TimeSpan?>("EndTime")
                        .HasColumnType("time");

                    b.Property<Guid>("PreferredPickUpDatesId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<TimeSpan?>("StartTime")
                        .HasColumnType("time");

                    b.HasKey("Id");

                    b.HasIndex("PreferredPickUpDatesId");

                    b.ToTable("timeRanges");
                });

            modelBuilder.Entity("Perikato.Data.User", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("Bank")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTime?>("Birthdate")
                        .HasColumnType("datetime2");

                    b.Property<DateTime>("CreatedDate")
                        .HasColumnType("datetime2");

                    b.Property<string>("DefaultVehicle")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Email")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("FamilyName")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("GivenName")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTime?>("LastAuthentication")
                        .HasColumnType("datetime2");

                    b.Property<string>("LoginToken")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Phonenumber")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("SSN")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Sub")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("TupasPID")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Username")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Id");

                    b.ToTable("Users");
                });

            modelBuilder.Entity("Perikato.Data.Carriers.MatchedDealIds", b =>
                {
                    b.HasOne("Perikato.Data.Carriers.Routes", "Routes")
                        .WithMany("matchedDealIds")
                        .HasForeignKey("RouteId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Routes");
                });

            modelBuilder.Entity("Perikato.Data.Carriers.RouteDates", b =>
                {
                    b.HasOne("Perikato.Data.Carriers.Routes", "Routes")
                        .WithMany("RouteDates")
                        .HasForeignKey("RouteId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Routes");
                });

            modelBuilder.Entity("Perikato.Data.Carriers.Routes", b =>
                {
                    b.HasOne("Perikato.Data.User", "User")
                        .WithMany("Routes")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("User");
                });

            modelBuilder.Entity("Perikato.Data.Dealers.Package", b =>
                {
                    b.HasOne("Perikato.Data.Dealers.DeliveryRequest", "DeliveryRequest")
                        .WithMany("Packages")
                        .HasForeignKey("DeliveryRequestId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("DeliveryRequest");
                });

            modelBuilder.Entity("Perikato.Data.Dealers.PreferredPickUpDates", b =>
                {
                    b.HasOne("Perikato.Data.Dealers.DeliveryRequest", "DeliveryRequest")
                        .WithMany("PickUpDates")
                        .HasForeignKey("DeliveryRequestId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("DeliveryRequest");
                });

            modelBuilder.Entity("Perikato.Data.Dealers.TimeRange", b =>
                {
                    b.HasOne("Perikato.Data.Dealers.PreferredPickUpDates", "PreferredPickUpDates")
                        .WithMany("PreferredTimeRanges")
                        .HasForeignKey("PreferredPickUpDatesId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("PreferredPickUpDates");
                });

            modelBuilder.Entity("Perikato.Data.Carriers.Routes", b =>
                {
                    b.Navigation("RouteDates");

                    b.Navigation("matchedDealIds");
                });

            modelBuilder.Entity("Perikato.Data.Dealers.DeliveryRequest", b =>
                {
                    b.Navigation("Packages");

                    b.Navigation("PickUpDates");
                });

            modelBuilder.Entity("Perikato.Data.Dealers.PreferredPickUpDates", b =>
                {
                    b.Navigation("PreferredTimeRanges");
                });

            modelBuilder.Entity("Perikato.Data.User", b =>
                {
                    b.Navigation("Routes");
                });
#pragma warning restore 612, 618
        }
    }
}
