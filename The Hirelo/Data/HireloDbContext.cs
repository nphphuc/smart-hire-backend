using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using The_Hirelo.Models;

namespace The_Hirelo.Data
{
    public class HireloDbContext : DbContext
    {
        public DbSet<User> Users => Set<User>();
        public DbSet<Company> Companies => Set<Company>();
        public DbSet<RecruiterProfile> RecruiterProfiles => Set<RecruiterProfile>();
        public DbSet<CandidateProfile> CandidateProfiles => Set<CandidateProfile>();
        public DbSet<Job> Jobs => Set<Job>();
        public DbSet<Application> Applications => Set<Application>();
        public DbSet<InterviewSession> InterviewSessions => Set<InterviewSession>();
        public DbSet<InterviewQuestion> InterviewQuestions => Set<InterviewQuestion>();
        public DbSet<InterviewAnswer> InterviewAnswers => Set<InterviewAnswer>();
        public DbSet<CodeSubmission> CodeSubmissions => Set<CodeSubmission>();
        public DbSet<CodeEvaluation> CodeEvaluations => Set<CodeEvaluation>();
        public DbSet<EmotionFrame> EmotionFrames => Set<EmotionFrame>();
        public DbSet<Scorecard> Scorecards => Set<Scorecard>();
        public DbSet<InterviewReport> InterviewReports => Set<InterviewReport>();
        public DbSet<Notification> Notifications => Set<Notification>();
        public DbSet<RecruiterVerification> RecruiterVerifications { get; set; }

        public HireloDbContext(DbContextOptions<HireloDbContext> options)
        : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            // Configure relationships and constraints here if needed
            modelBuilder.Entity<User>()
                .Property(u => u.Role)
                .HasConversion<string>();
            modelBuilder.Entity<User>()
                .HasIndex(u => u.CognitoSub)
                .IsUnique();
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            modelBuilder.Entity<InterviewSession>()
                .Property(x => x.Status)
                .HasConversion<string>();

            modelBuilder.Entity<Application>()
                .Property(a => a.Status)
                .HasConversion<string>();

            modelBuilder.Entity<Application>()
                .HasOne(a => a.Job)
                .WithMany()
                .HasForeignKey(a => a.JobId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Application>()
                .HasOne(a => a.Candidate)
                .WithMany()
                .HasForeignKey(a => a.CandidateId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Application>()
                .HasIndex(a => new { a.JobId, a.CandidateId })
                .IsUnique();

            modelBuilder.Entity<RecruiterVerification>()
                .HasOne(v => v.User)
                .WithMany()
                .HasForeignKey(v => v.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<RecruiterVerification>()
                .Property(v => v.ImagesJson)
                .HasColumnType("jsonb");
        }
    }
}
