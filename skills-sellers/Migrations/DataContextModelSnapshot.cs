﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using skills_sellers.Helpers.Bdd;

#nullable disable

namespace skills_sellers.Migrations
{
    [DbContext(typeof(DataContext))]
    partial class DataContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "7.0.11")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

            modelBuilder.Entity("skills_sellers.Entities.Action", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<string>("ActionType")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<DateTime>("DueDate")
                        .HasColumnType("timestamp without time zone");

                    b.Property<int>("UserId")
                        .HasColumnType("integer");

                    b.HasKey("Id");

                    b.HasIndex("UserId");

                    b.ToTable("Actions");

                    b.HasDiscriminator<string>("ActionType").HasValue("Action");

                    b.UseTphMappingStrategy();
                });

            modelBuilder.Entity("skills_sellers.Entities.AuthUser", b =>
                {
                    b.Property<int>("UserId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("UserId"));

                    b.Property<string>("Hash")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("Role")
                        .IsRequired()
                        .HasColumnType("text");

                    b.HasKey("UserId");

                    b.ToTable("auth_users");
                });

            modelBuilder.Entity("skills_sellers.Entities.Card", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<string>("Collection")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("Description")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("Rarity")
                        .IsRequired()
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.ToTable("cards");
                });

            modelBuilder.Entity("skills_sellers.Entities.Competences", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<int>("Charisme")
                        .HasColumnType("integer");

                    b.Property<int>("Cuisine")
                        .HasColumnType("integer");

                    b.Property<int>("Exploration")
                        .HasColumnType("integer");

                    b.Property<int>("Force")
                        .HasColumnType("integer");

                    b.Property<int>("Intelligence")
                        .HasColumnType("integer");

                    b.HasKey("Id");

                    b.ToTable("Competences");
                });

            modelBuilder.Entity("skills_sellers.Entities.DailyTaskLog", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<DateTime>("ExecutionDate")
                        .HasColumnType("timestamp without time zone");

                    b.Property<bool>("IsExecuted")
                        .HasColumnType("boolean");

                    b.HasKey("Id");

                    b.ToTable("DailyTaskLog");
                });

            modelBuilder.Entity("skills_sellers.Entities.GiftCode", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<string>("Code")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<int>("NbCards")
                        .HasColumnType("integer");

                    b.Property<int>("NbCreatium")
                        .HasColumnType("integer");

                    b.Property<int>("NbOr")
                        .HasColumnType("integer");

                    b.Property<bool>("Used")
                        .HasColumnType("boolean");

                    b.HasKey("Id");

                    b.ToTable("GiftCodes");
                });

            modelBuilder.Entity("skills_sellers.Entities.Notification", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("timestamp without time zone");

                    b.Property<string>("Message")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("Title")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<int>("UserId")
                        .HasColumnType("integer");

                    b.HasKey("Id");

                    b.HasIndex("UserId");

                    b.ToTable("Notifications");
                });

            modelBuilder.Entity("skills_sellers.Entities.Stats", b =>
                {
                    b.Property<int>("UserId")
                        .HasColumnType("integer");

                    b.Property<int>("TotalBuildingsUpgraded")
                        .HasColumnType("integer");

                    b.Property<int>("TotalCreatiumMined")
                        .HasColumnType("integer");

                    b.Property<int>("TotalDoublonsEarned")
                        .HasColumnType("integer");

                    b.Property<int>("TotalFailedCardsCauseOfCharisme")
                        .HasColumnType("integer");

                    b.Property<int>("TotalLooseAtCharismeCasino")
                        .HasColumnType("integer");

                    b.Property<int>("TotalMachineUsed")
                        .HasColumnType("integer");

                    b.Property<int>("TotalMealCooked")
                        .HasColumnType("integer");

                    b.Property<int>("TotalMessagesSent")
                        .HasColumnType("integer");

                    b.Property<int>("TotalOrMined")
                        .HasColumnType("integer");

                    b.Property<int>("TotalRocketLaunched")
                        .HasColumnType("integer");

                    b.Property<int>("TotalWinAtCharismeCasino")
                        .HasColumnType("integer");

                    b.HasKey("UserId");

                    b.ToTable("Stats");
                });

            modelBuilder.Entity("skills_sellers.Entities.User", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<int>("Creatium")
                        .HasColumnType("integer");

                    b.Property<int>("NbCardOpeningAvailable")
                        .HasColumnType("integer");

                    b.Property<int>("Nourriture")
                        .HasColumnType("integer");

                    b.Property<int>("Or")
                        .HasColumnType("integer");

                    b.Property<string>("Pseudo")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<int>("StatRepairedObjectMachine")
                        .HasColumnType("integer");

                    b.HasKey("Id");

                    b.ToTable("users");
                });

            modelBuilder.Entity("skills_sellers.Entities.UserBatimentData", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<int>("CuisineLevel")
                        .HasColumnType("integer");

                    b.Property<int>("LaboLevel")
                        .HasColumnType("integer");

                    b.Property<int>("NbBuyMarchandToday")
                        .HasColumnType("integer");

                    b.Property<int>("NbCuisineUsedToday")
                        .HasColumnType("integer");

                    b.Property<int>("SalleSportLevel")
                        .HasColumnType("integer");

                    b.Property<int>("SpatioPortLevel")
                        .HasColumnType("integer");

                    b.Property<int>("UserId")
                        .HasColumnType("integer");

                    b.HasKey("Id");

                    b.HasIndex("UserId")
                        .IsUnique();

                    b.ToTable("UserBatiments");
                });

            modelBuilder.Entity("skills_sellers.Entities.UserCard", b =>
                {
                    b.Property<int>("UserId")
                        .HasColumnType("integer");

                    b.Property<int>("CardId")
                        .HasColumnType("integer");

                    b.Property<int?>("ActionId")
                        .HasColumnType("integer");

                    b.Property<int>("CompetencesId")
                        .HasColumnType("integer");

                    b.HasKey("UserId", "CardId");

                    b.HasIndex("ActionId");

                    b.HasIndex("CardId");

                    b.HasIndex("CompetencesId");

                    b.ToTable("UserCards");
                });

            modelBuilder.Entity("skills_sellers.Entities.UserCardDoubled", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<int>("CardId")
                        .HasColumnType("integer");

                    b.Property<int>("UserId")
                        .HasColumnType("integer");

                    b.HasKey("Id");

                    b.HasIndex("CardId");

                    b.HasIndex("UserId");

                    b.ToTable("UserCardDoubleds");
                });

            modelBuilder.Entity("skills_sellers.Entities.Actions.ActionAmeliorer", b =>
                {
                    b.HasBaseType("skills_sellers.Entities.Action");

                    b.Property<string>("BatimentToUpgrade")
                        .IsRequired()
                        .HasColumnType("text");

                    b.HasDiscriminator().HasValue("Ameliorer");
                });

            modelBuilder.Entity("skills_sellers.Entities.Actions.ActionCuisiner", b =>
                {
                    b.HasBaseType("skills_sellers.Entities.Action");

                    b.Property<string>("Plat")
                        .IsRequired()
                        .HasColumnType("text");

                    b.HasDiscriminator().HasValue("Cuisine");
                });

            modelBuilder.Entity("skills_sellers.Entities.Actions.ActionExplorer", b =>
                {
                    b.HasBaseType("skills_sellers.Entities.Action");

                    b.Property<bool>("IsReturningToHome")
                        .HasColumnType("boolean");

                    b.Property<string>("PlanetName")
                        .IsRequired()
                        .HasColumnType("text");

                    b.HasDiscriminator().HasValue("Explorer");
                });

            modelBuilder.Entity("skills_sellers.Entities.Actions.ActionMuscler", b =>
                {
                    b.HasBaseType("skills_sellers.Entities.Action");

                    b.Property<string>("Muscle")
                        .IsRequired()
                        .HasColumnType("text");

                    b.HasDiscriminator().HasValue("Muscler");
                });

            modelBuilder.Entity("skills_sellers.Entities.Actions.ActionReparer", b =>
                {
                    b.HasBaseType("skills_sellers.Entities.Action");

                    b.Property<double?>("RepairChances")
                        .HasColumnType("double precision");

                    b.HasDiscriminator().HasValue("Reparer");
                });

            modelBuilder.Entity("skills_sellers.Entities.Action", b =>
                {
                    b.HasOne("skills_sellers.Entities.User", "User")
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("User");
                });

            modelBuilder.Entity("skills_sellers.Entities.Notification", b =>
                {
                    b.HasOne("skills_sellers.Entities.User", "User")
                        .WithMany("Notifications")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("User");
                });

            modelBuilder.Entity("skills_sellers.Entities.Stats", b =>
                {
                    b.HasOne("skills_sellers.Entities.User", "User")
                        .WithOne("Stats")
                        .HasForeignKey("skills_sellers.Entities.Stats", "UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("User");
                });

            modelBuilder.Entity("skills_sellers.Entities.UserBatimentData", b =>
                {
                    b.HasOne("skills_sellers.Entities.User", "User")
                        .WithOne("UserBatimentData")
                        .HasForeignKey("skills_sellers.Entities.UserBatimentData", "UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("User");
                });

            modelBuilder.Entity("skills_sellers.Entities.UserCard", b =>
                {
                    b.HasOne("skills_sellers.Entities.Action", "Action")
                        .WithMany("UserCards")
                        .HasForeignKey("ActionId");

                    b.HasOne("skills_sellers.Entities.Card", "Card")
                        .WithMany("UserCards")
                        .HasForeignKey("CardId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("skills_sellers.Entities.Competences", "Competences")
                        .WithMany()
                        .HasForeignKey("CompetencesId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("skills_sellers.Entities.User", "User")
                        .WithMany("UserCards")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Action");

                    b.Navigation("Card");

                    b.Navigation("Competences");

                    b.Navigation("User");
                });

            modelBuilder.Entity("skills_sellers.Entities.UserCardDoubled", b =>
                {
                    b.HasOne("skills_sellers.Entities.Card", "Card")
                        .WithMany()
                        .HasForeignKey("CardId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("skills_sellers.Entities.User", "User")
                        .WithMany("UserCardsDoubled")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Card");

                    b.Navigation("User");
                });

            modelBuilder.Entity("skills_sellers.Entities.Action", b =>
                {
                    b.Navigation("UserCards");
                });

            modelBuilder.Entity("skills_sellers.Entities.Card", b =>
                {
                    b.Navigation("UserCards");
                });

            modelBuilder.Entity("skills_sellers.Entities.User", b =>
                {
                    b.Navigation("Notifications");

                    b.Navigation("Stats")
                        .IsRequired();

                    b.Navigation("UserBatimentData")
                        .IsRequired();

                    b.Navigation("UserCards");

                    b.Navigation("UserCardsDoubled");
                });
#pragma warning restore 612, 618
        }
    }
}
