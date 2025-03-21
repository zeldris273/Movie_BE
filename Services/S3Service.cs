using System;
using System.IO;
using System.Threading.Tasks;
using Amazon.S3;
using Amazon.S3.Model;

namespace backend.Services
{
    public class S3Service
    {
        private readonly IAmazonS3 _s3Client;
        private const string BucketName = "my-movie-app";

        public S3Service(IAmazonS3 s3Client)
        {
            _s3Client = s3Client;
        }

        public async Task<string> UploadFileAsync(Stream fileStream, string fileName, string folder, string contentType)
        {
            var request = new PutObjectRequest
            {
                BucketName = BucketName,
                Key = $"{folder}/{fileName}",
                InputStream = fileStream,
                ContentType = contentType
            };

            await _s3Client.PutObjectAsync(request);
            return $"https://{BucketName}.s3.amazonaws.com/{folder}/{fileName}";
        }
    }
}
