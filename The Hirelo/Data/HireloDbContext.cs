using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using The_Hirelo.Models;

namespace The_Hirelo.Data;

public partial class HireloDbContext : DbContext
{
    public HireloDbContext()
    {
    }

    public HireloDbContext(DbContextOptions<HireloDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<CandidateProfile> CandidateProfiles { get; set; }

    public virtual DbSet<CodeEvaluation> CodeEvaluations { get; set; }

    public virtual DbSet<CodeSubmission> CodeSubmissions { get; set; }

    public virtual DbSet<Company> Companies { get; set; }

    public virtual DbSet<EmotionFrame> EmotionFrames { get; set; }

    public virtual DbSet<InterviewAnswer> InterviewAnswers { get; set; }

    public virtual DbSet<InterviewQuestion> InterviewQuestions { get; set; }

    public virtual DbSet<InterviewReport> InterviewReports { get; set; }

    public virtual DbSet<InterviewSession> InterviewSessions { get; set; }

    public virtual DbSet<Job> Jobs { get; set; }

    public virtual DbSet<Notification> Notifications { get; set; }

    public virtual DbSet<RecruiterProfile> RecruiterProfiles { get; set; }

    public virtual DbSet<Scorecard> Scorecards { get; set; }

    public virtual DbSet<User> Users { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseNpgsql("Host=localhost;Database=hirelo;Username=postgres;Password=12345");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CandidateProfile>(entity =>
        {
            entity.HasIndex(e => e.UserId, "IX_CandidateProfiles_UserId").IsUnique();

            entity.HasIndex(e => new { e.JobId, e.MatchingScore }, "idx_candidate_profiles_jobid_score").IsDescending(false, true);

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone");
            entity.Property(e => e.Status).HasDefaultValueSql("'PENDING'::text");
            entity.Property(e => e.UpdatedAt).HasColumnType("timestamp without time zone");

            entity.HasOne(d => d.Job).WithMany(p => p.CandidateProfiles)
                .HasForeignKey(d => d.JobId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("CandidateProfiles_JobId_fkey");

            entity.HasOne(d => d.User).WithOne(p => p.CandidateProfile).HasForeignKey<CandidateProfile>(d => d.UserId);
        });

        modelBuilder.Entity<CodeEvaluation>(entity =>
        {
            entity.HasIndex(e => e.SubmissionId, "IX_CodeEvaluations_SubmissionId").IsUnique();

            entity.Property(e => e.Id).ValueGeneratedNever();

            entity.HasOne(d => d.Submission).WithOne(p => p.CodeEvaluation).HasForeignKey<CodeEvaluation>(d => d.SubmissionId);
        });

        modelBuilder.Entity<CodeSubmission>(entity =>
        {
            entity.HasIndex(e => e.SessionId, "IX_CodeSubmissions_SessionId");

            entity.Property(e => e.Id).ValueGeneratedNever();

            entity.HasOne(d => d.Session).WithMany(p => p.CodeSubmissions).HasForeignKey(d => d.SessionId);
        });

        modelBuilder.Entity<Company>(entity =>
        {
            entity.Property(e => e.Id).ValueGeneratedNever();
        });

        modelBuilder.Entity<EmotionFrame>(entity =>
        {
            entity.HasIndex(e => e.SessionId, "IX_EmotionFrames_SessionId");

            entity.Property(e => e.Id).ValueGeneratedNever();

            entity.HasOne(d => d.Session).WithMany(p => p.EmotionFrames).HasForeignKey(d => d.SessionId);
        });

        modelBuilder.Entity<InterviewAnswer>(entity =>
        {
            entity.HasIndex(e => e.QuestionId, "IX_InterviewAnswers_QuestionId");

            entity.Property(e => e.Id).ValueGeneratedNever();

            entity.HasOne(d => d.Question).WithMany(p => p.InterviewAnswers).HasForeignKey(d => d.QuestionId);
        });

        modelBuilder.Entity<InterviewQuestion>(entity =>
        {
            entity.HasIndex(e => e.SessionId, "IX_InterviewQuestions_SessionId");

            entity.Property(e => e.Id).ValueGeneratedNever();

            entity.HasOne(d => d.Session).WithMany(p => p.InterviewQuestions).HasForeignKey(d => d.SessionId);
        });

        modelBuilder.Entity<InterviewReport>(entity =>
        {
            entity.HasIndex(e => e.SessionId, "IX_InterviewReports_SessionId").IsUnique();

            entity.Property(e => e.Id).ValueGeneratedNever();

            entity.HasOne(d => d.Session).WithOne(p => p.InterviewReport).HasForeignKey<InterviewReport>(d => d.SessionId);
        });

        modelBuilder.Entity<InterviewSession>(entity =>
        {
            entity.HasIndex(e => e.CandidateId, "IX_InterviewSessions_CandidateId");

            entity.HasIndex(e => e.JobId, "IX_InterviewSessions_JobId");

            entity.Property(e => e.Id).ValueGeneratedNever();

            entity.HasOne(d => d.Candidate).WithMany(p => p.InterviewSessions).HasForeignKey(d => d.CandidateId);

            entity.HasOne(d => d.Job).WithMany(p => p.InterviewSessions).HasForeignKey(d => d.JobId);
        });

        modelBuilder.Entity<Job>(entity =>
        {
            entity.HasIndex(e => e.RecruiterId, "IX_Jobs_RecruiterId");

            entity.Property(e => e.Id).ValueGeneratedNever();

            entity.HasOne(d => d.Recruiter).WithMany(p => p.Jobs).HasForeignKey(d => d.RecruiterId);
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasIndex(e => e.UserId, "IX_Notifications_UserId");

            entity.Property(e => e.Id).ValueGeneratedNever();

            entity.HasOne(d => d.User).WithMany(p => p.Notifications).HasForeignKey(d => d.UserId);
        });

        modelBuilder.Entity<RecruiterProfile>(entity =>
        {
            entity.HasIndex(e => e.CompanyId, "IX_RecruiterProfiles_CompanyId");

            entity.HasIndex(e => e.UserId, "IX_RecruiterProfiles_UserId").IsUnique();

            entity.Property(e => e.Id).ValueGeneratedNever();

            entity.HasOne(d => d.Company).WithMany(p => p.RecruiterProfiles).HasForeignKey(d => d.CompanyId);

            entity.HasOne(d => d.User).WithOne(p => p.RecruiterProfile).HasForeignKey<RecruiterProfile>(d => d.UserId);
        });

        modelBuilder.Entity<Scorecard>(entity =>
        {
            entity.HasIndex(e => e.SessionId, "IX_Scorecards_SessionId").IsUnique();

            entity.Property(e => e.Id).ValueGeneratedNever();

            entity.HasOne(d => d.Session).WithOne(p => p.Scorecard).HasForeignKey<Scorecard>(d => d.SessionId);
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.Property(e => e.Id).ValueGeneratedNever();
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
