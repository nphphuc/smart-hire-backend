using The_Hirelo.Enums;
using The_Hirelo.Models;

namespace The_Hirelo.Repositories.Interfaces
{
    public interface IUserRepository
    {
        Task<User?> GetByIdAsync(Guid id);
        Task<User?> GetByEmailAsync(string email);
        Task<User?> GetByCognitoSubAsync(string sub); // Lấy user theo sub từ Cognito
        Task<User> CreateAsync(User user);
        Task<IEnumerable<User>> GetByRoleAsync(UserRole role);
        Task<User> UpdateAsync(Guid id, User user); 
        Task UpdateAsync(User user);
        Task DeleteAsync(Guid id);
    }
}
