﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace LMPWebService.Migrations
{
    [DbContext(typeof(AstraDbContext))]
    partial class AstraDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "9.0.2")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

            modelBuilder.Entity("LMPWebService.Models.OuterMessage", b =>
                {
                    b.Property<Guid>("OuterMessage_ID")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid")
                        .HasDefaultValueSql("gen_random_uuid()");

                    b.Property<int?>("ErrorCode")
                        .HasColumnType("integer");

                    b.Property<string>("ErrorMessage")
                        .IsRequired()
                        .HasMaxLength(255)
                        .HasColumnType("character varying(255)");

                    b.Property<DateTime>("InsDate")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("timestamp with time zone")
                        .HasDefaultValueSql("CURRENT_TIMESTAMP");

                    b.Property<string>("MessageOuter_ID")
                        .IsRequired()
                        .HasMaxLength(255)
                        .HasColumnType("character varying(255)");

                    b.Property<string>("MessageText")
                        .IsRequired()
                        .HasMaxLength(255)
                        .HasColumnType("character varying(255)");

                    b.Property<int>("OuterMessageReader_ID")
                        .HasMaxLength(255)
                        .HasColumnType("integer");

                    b.Property<byte>("ProcessingStatus")
                        .HasMaxLength(255)
                        .HasColumnType("smallint");

                    b.Property<DateTime?>("UpdDate")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("timestamp with time zone")
                        .HasDefaultValueSql("CURRENT_TIMESTAMP");

                    b.HasKey("OuterMessage_ID");

                    b.ToTable("OuterMessage", (string)null);
                });

            modelBuilder.Entity("LMPWebService.Models.OuterMessageReader", b =>
                {
                    b.Property<int>("OuterMessageReader_ID")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("OuterMessageReader_ID"));

                    b.Property<Guid>("InsApplicationUser_ID")
                        .HasMaxLength(255)
                        .HasColumnType("uuid");

                    b.Property<DateTime?>("InsDate")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("timestamp with time zone")
                        .HasDefaultValueSql("CURRENT_TIMESTAMP");

                    b.Property<DateTime?>("LastSuccessReadDate")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("timestamp with time zone")
                        .HasDefaultValueSql("CURRENT_TIMESTAMP");

                    b.Property<string>("OuterMessageReaderName")
                        .IsRequired()
                        .HasMaxLength(255)
                        .HasColumnType("character varying(255)");

                    b.Property<string>("OuterMessageSourceName")
                        .IsRequired()
                        .HasMaxLength(255)
                        .HasColumnType("character varying(255)");

                    b.Property<int>("OuterSystem_ID")
                        .HasMaxLength(255)
                        .HasColumnType("integer");

                    b.Property<Guid>("UpdApplicationUser_ID")
                        .HasMaxLength(255)
                        .HasColumnType("uuid");

                    b.Property<DateTime?>("UpdDate")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("timestamp with time zone")
                        .HasDefaultValueSql("CURRENT_TIMESTAMP");

                    b.HasKey("OuterMessageReader_ID");

                    b.ToTable("OuterMessageReader", (string)null);
                });
#pragma warning restore 612, 618
        }
    }
}
