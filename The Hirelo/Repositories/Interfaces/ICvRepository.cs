using The_Hirelo.DTOs.Responses;

namespace The_Hirelo.Repositories.Interfaces
{
    public interface ICvRepository
    {
        /// <summary>
        /// Lấy bản ghi CV mới nhất của candidate dựa trên candidateId (Partition Key).
        /// Trả về item có objectKey mới nhất (sort theo ProcessedAt giảm dần).
        /// </summary>
        Task<CvParseResultResponse?> GetLatestCvByCandidateIdAsync(string candidateId);

        /// <summary>
        /// Lấy tất cả các bản ghi CV của candidate (trường hợp candidate upload nhiều file).
        /// </summary>
        Task<IEnumerable<CvParseResultResponse>> GetAllCvsByCandidateIdAsync(string candidateId);
    }
}
