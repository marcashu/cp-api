﻿// <auto-generated> This file has been auto generated by EF Core Power Tools. </auto-generated>
#nullable disable
using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace CottonPrompt.Infrastructure.Entities;

public partial class CottonPromptContext : DbContext
{
    public CottonPromptContext(DbContextOptions<CottonPromptContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Order> Orders { get; set; }

    public virtual DbSet<OrderDesignBracket> OrderDesignBrackets { get; set; }

    public virtual DbSet<OrderImageReference> OrderImageReferences { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_OrderId");

            entity.Property(e => e.Concept).IsRequired();
            entity.Property(e => e.CreatedOn).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.Number)
                .IsRequired()
                .HasMaxLength(50);
            entity.Property(e => e.PrintColor)
                .IsRequired()
                .HasMaxLength(50);

            entity.HasOne(d => d.DesignBracket).WithMany(p => p.Orders)
                .HasForeignKey(d => d.DesignBracketId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Orders_OrderDesignBrackets");
        });

        modelBuilder.Entity<OrderDesignBracket>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_OrderDesignBracketId");

            entity.Property(e => e.Value).HasColumnType("decimal(19, 4)");
        });

        modelBuilder.Entity<OrderImageReference>(entity =>
        {
            entity.HasKey(e => new { e.OrderId, e.LineId }).HasName("PK_OrderImageReferences_OrderID_LineId");

            entity.Property(e => e.Url).IsRequired();

            entity.HasOne(d => d.Order).WithMany(p => p.OrderImageReferences)
                .HasForeignKey(d => d.OrderId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OrderImageReferences_Orders");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}