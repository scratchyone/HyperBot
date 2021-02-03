﻿// <auto-generated />
using System;
using HyperBot.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace HyperBot.Migrations
{
    [DbContext(typeof(DataContext))]
    partial class DataContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "5.0.2");

            modelBuilder.Entity("HyperBot.Models.IPGrabberUrl", b =>
                {
                    b.Property<string>("Domain")
                        .HasColumnType("TEXT");

                    b.HasKey("Domain");

                    b.ToTable("IPGrabberUrls");
                });

            modelBuilder.Entity("HyperBot.Models.PagerItem", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<ulong>("Author")
                        .HasColumnType("INTEGER");

                    b.Property<ulong?>("Guild")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Text")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.ToTable("Pagers");
                });

            modelBuilder.Entity("HyperBot.Models.PinboardItem", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<ulong>("Author")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Text")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("Timestamp")
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.ToTable("PinboardItems");
                });

            modelBuilder.Entity("HyperBot.Models.Prefix", b =>
                {
                    b.Property<string>("PrefixText")
                        .HasColumnType("TEXT");

                    b.Property<ulong>("Guild")
                        .HasColumnType("INTEGER");

                    b.HasKey("PrefixText", "Guild");

                    b.ToTable("Prefixes");
                });

            modelBuilder.Entity("HyperBot.Models.PronounSet", b =>
                {
                    b.Property<string>("Set")
                        .HasColumnType("TEXT");

                    b.HasKey("Set");

                    b.ToTable("Pronouns");
                });

            modelBuilder.Entity("HyperBot.Models.ServerProtectGuild", b =>
                {
                    b.Property<ulong>("Guild")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.HasKey("Guild");

                    b.ToTable("ServerProtectGuilds");
                });

            modelBuilder.Entity("HyperBot.Models.ServerProtectUnsafeFile", b =>
                {
                    b.Property<string>("Hash")
                        .HasColumnType("TEXT");

                    b.Property<string>("Description")
                        .HasColumnType("TEXT");

                    b.HasKey("Hash");

                    b.ToTable("UnsafeFiles");
                });
#pragma warning restore 612, 618
        }
    }
}
