using The_Hirelo.Data;
using The_Hirelo.Models;
using The_Hirelo.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace The_Hirelo.Repositories
{
    public class ReportRepository : IReportRepository
    {
        private readonly HireloDbContext _context;
        public ReportRepository(HireloDbContext context)
        {
            _context = context;
        }

        public async Task<InterviewReport?> GetByInterviewIdAsync(Guid interviewId)
        {
            return await _context.InterviewReports.FirstOrDefaultAsync(r => r.SessionId == interviewId);
        }

        public async Task<InterviewReport> SaveAsync(InterviewReport report)
        {
            _context.InterviewReports.Add(report);
            await _context.SaveChangesAsync();
            return report;
        }

        public async Task<InterviewReport?> GetByIdAsync(Guid id)
        {
            return await _context.InterviewReports.FindAsync(id);
        }

        public async Task<string> GetHtmlContentAsync(Guid reportId)
        {
            var report = await GetByIdAsync(reportId);
            return report?.ReportHtml ?? string.Empty;
        }

        public async Task<string> GetJsonContentAsync(Guid reportId)
        {
            var report = await GetByIdAsync(reportId);
            return report?.ReportJson ?? string.Empty;
        }
    }
}
