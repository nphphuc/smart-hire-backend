using The_Hirelo.Enums;

namespace The_Hirelo.DTOs.Requests
{
    public class UpdateRoleRequest
    {
        public UserRole Role { get; set; } = UserRole.Candidate!;
    }
}
