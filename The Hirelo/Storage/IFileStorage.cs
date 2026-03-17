namespace The_Hirelo.Storage
{
    public interface IFileStorage
    {
        Task<string> UploadAsync(IFormFile file, string key);
    }
}
