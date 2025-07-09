using Microsoft.EntityFrameworkCore;
using OrderAuditTrail.Shared.Models;

namespace OrderAuditTrail.Shared.Data;

/// <summary>
/// Entity Framework Core DbContext for the audit trail system
/// </summary>
public class AuditDbContext : DbContext
{
    public AuditDbContext(DbContextOptions<AuditDbContext> options) : base(options)
    {
    }

    /// <summary>
    /// Events table - stores all business events
    /// </summary>
    public DbSet<EventEntity> Events { get; set; }

    /// <summary>
    /// Replay operations table - tracks event replay operations
    /// </summary>
    public DbSet<ReplayOperationEntity> ReplayOperations { get; set; }

    /// <summary>
    /// Dead letter queue table - stores failed event processing attempts
    /// </summary>
    public DbSet<DeadLetterQueueEntity> DeadLetterQueue { get; set; }

    /// <summary>
    /// Metrics table - stores system metrics and performance data
    /// </summary>
    public DbSet<MetricsEntity> Metrics { get; set; }

    /// <summary>
    /// Audit log table - stores system audit logs
    /// </summary>
    public DbSet<AuditLogEntity> AuditLogs { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure Events table
        modelBuilder.Entity<EventEntity>(entity =>
        {
            entity.ToTable("events");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.EventId).IsUnique();
            entity.HasIndex(e => new { e.AggregateId, e.Version }).IsUnique();
            entity.HasIndex(e => e.EventType);
            entity.HasIndex(e => e.AggregateType);
            entity.HasIndex(e => e.Timestamp);
            entity.HasIndex(e => e.CreatedAt);

            entity.Property(e => e.EventId).IsRequired();
            entity.Property(e => e.EventType).IsRequired().HasMaxLength(255);
            entity.Property(e => e.AggregateId).IsRequired().HasMaxLength(255);
            entity.Property(e => e.AggregateType).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Payload).HasColumnType("jsonb");
            entity.Property(e => e.Metadata).HasColumnType("jsonb");
        });

        // Configure ReplayOperations table
        modelBuilder.Entity<ReplayOperationEntity>(entity =>
        {
            entity.ToTable("replay_operations");
            entity.HasKey(r => r.Id);
            entity.HasIndex(r => r.ReplayId).IsUnique();
            entity.HasIndex(r => r.Status);
            entity.HasIndex(r => r.CreatedAt);

            entity.Property(r => r.ReplayId).IsRequired();
            entity.Property(r => r.Status).IsRequired().HasMaxLength(50);
            entity.Property(r => r.FilterCriteria).HasColumnType("jsonb");
            entity.Property(r => r.ErrorMessage).HasMaxLength(2000);
        });

        // Configure DeadLetterQueue table
        modelBuilder.Entity<DeadLetterQueueEntity>(entity =>
        {
            entity.ToTable("dead_letter_queue");
            entity.HasKey(d => d.Id);
            entity.HasIndex(d => d.OriginalTopic);
            entity.HasIndex(d => d.ErrorType);
            entity.HasIndex(d => d.CreatedAt);

            entity.Property(d => d.OriginalTopic).IsRequired().HasMaxLength(255);
            entity.Property(d => d.ErrorType).IsRequired().HasMaxLength(255);
            entity.Property(d => d.ErrorMessage).HasMaxLength(2000);
            entity.Property(d => d.OriginalPayload).HasColumnType("jsonb");
            entity.Property(d => d.Metadata).HasColumnType("jsonb");
        });

        // Configure Metrics table
        modelBuilder.Entity<MetricsEntity>(entity =>
        {
            entity.ToTable("metrics");
            entity.HasKey(m => m.Id);
            entity.HasIndex(m => m.MetricName);
            entity.HasIndex(m => m.Timestamp);
            entity.HasIndex(m => new { m.MetricName, m.Timestamp });

            entity.Property(m => m.MetricName).IsRequired().HasMaxLength(255);
            entity.Property(m => m.Value).HasPrecision(18, 4);
            entity.Property(m => m.Tags).HasColumnType("jsonb");
            entity.Property(m => m.Unit).HasMaxLength(50);
        });

        // Configure AuditLogs table
        modelBuilder.Entity<AuditLogEntity>(entity =>
        {
            entity.ToTable("audit_logs");
            entity.HasKey(a => a.Id);
            entity.HasIndex(a => a.Action);
            entity.HasIndex(a => a.UserId);
            entity.HasIndex(a => a.Timestamp);

            entity.Property(a => a.Action).IsRequired().HasMaxLength(255);
            entity.Property(a => a.UserId).HasMaxLength(255);
            entity.Property(a => a.ResourceId).HasMaxLength(255);
            entity.Property(a => a.ResourceType).HasMaxLength(255);
            entity.Property(a => a.Details).HasColumnType("jsonb");
            entity.Property(a => a.IpAddress).HasMaxLength(45); // IPv6 max length
            entity.Property(a => a.UserAgent).HasMaxLength(1000);
        });
    }
}
