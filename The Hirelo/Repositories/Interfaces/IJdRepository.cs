using The_Hirelo.DTOs.Responses;

namespace The_Hirelo.Repositories.Interfaces
{
    public interface IJdRepository
    {
        /// <summary>
        /// Lấy bản ghi JD parse result mới nhất theo jobId (Partition Key).
        /// </summary>
        Task<JdParseResultResponse?> GetJdByJobIdAsync(string jobId);

        /// <summary>
        /// Lấy tất cả bản ghi JD parse result trong bảng (Scan).
        /// </summary>
        Task<IEnumerable<JdParseResultResponse>> GetAllJdsAsync();
    }
}
