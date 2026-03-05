using The_Hirelo.Models;

namespace The_Hirelo.Repositories.Interfaces
{
    public interface IUserRepository
    {
        Task<User?> GetByIdAsync(Guid id);
        Task<User?> GetByEmailAsync(string email);
        Task<User> UpdateAsync(Guid id, User user); 
        Task DeleteAsync(Guid id);
    }
}
