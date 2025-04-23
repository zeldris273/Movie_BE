using System;
using System.Threading.Tasks;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.AspNetCore.Http;

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

        public async Task<string> UploadFileAsync(IFormFile file, string folder)
        {
            if (file == null || file.Length == 0)
            {
                throw new ArgumentException("File không hợp lệ.");
            }

            // Tạo tên file duy nhất
            var fileName = file.FileName;
            var key = $"{folder}/{fileName}";

            // Lấy ContentType từ IFormFile
            var contentType = file.ContentType;

            // Mở stream từ IFormFile
            using var stream = file.OpenReadStream();

            // Tạo request để upload lên S3
            var request = new PutObjectRequest
            {
                BucketName = BucketName,
                Key = key,
                InputStream = stream,
                ContentType = contentType,
                CannedACL = S3CannedACL.PublicRead
            };

            // Upload file lên S3
            await _s3Client.PutObjectAsync(request);

            // Trả về URL của file trên S3
            return $"https://{BucketName}.s3.amazonaws.com/{key}";
        }
    }
}