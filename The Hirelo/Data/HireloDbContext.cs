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
        public DbSet<InterviewSession> InterviewSessions => Set<InterviewSession>();
        public DbSet<InterviewQuestion> InterviewQuestions => Set<InterviewQuestion>();
        public DbSet<InterviewAnswer> InterviewAnswers => Set<InterviewAnswer>();
        public DbSet<CodeSubmission> CodeSubmissions => Set<CodeSubmission>();
        public DbSet<CodeEvaluation> CodeEvaluations => Set<CodeEvaluation>();
        public DbSet<EmotionFrame> EmotionFrames => Set<EmotionFrame>();
        public DbSet<Scorecard> Scorecards => Set<Scorecard>();
        public DbSet<InterviewReport> InterviewReports => Set<InterviewReport>();
        public DbSet<Notification> Notifications => Set<Notification>();

        public HireloDbContext(DbContextOptions<HireloDbContext> options)
        : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            // Configure relationships and constraints here if needed
            modelBuilder.Entity<User>()
                .Property(u => u.Role)
                .HasConversion<string>();
        }
    }
}
