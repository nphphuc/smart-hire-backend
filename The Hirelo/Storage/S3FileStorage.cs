
using Amazon.S3;
using Amazon.S3.Model;

namespace The_Hirelo.Storage
{
    public class S3FileStorage : IFileStorage
    {
        private readonly IAmazonS3 _s3;
        private readonly string _bucket = "hirelo-media";

        public S3FileStorage(IAmazonS3 s3)
        {
            _s3 = s3;
        }

        public async Task<string> UploadAsync(IFormFile file, string key)
        {
            using var stream = file.OpenReadStream();

            var request = new PutObjectRequest
            {
                BucketName = _bucket,
                Key = key,
                InputStream = stream,
                ContentType = file.ContentType
            };

            await _s3.PutObjectAsync(request);

            return $"https://{_bucket}.s3.amazonaws.com/{key}";
        }
    }
}
