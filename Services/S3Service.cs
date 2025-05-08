using System;
using System.IO;
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

        public async Task<string> UploadHlsFolderAsync(string localFolderPath, string s3Folder)
        {
            if (string.IsNullOrEmpty(localFolderPath) || !Directory.Exists(localFolderPath))
                throw new ArgumentException("Thư mục nguồn không tồn tại.");

            // Đảm bảo thư mục S3 không kết thúc bằng "/"
            s3Folder = s3Folder.TrimEnd('/');

            try
            {
                // Lấy tất cả các file trong thư mục
                var files = Directory.GetFiles(localFolderPath, "*.*", SearchOption.TopDirectoryOnly);

                if (files.Length == 0)
                    throw new ArgumentException("Thư mục không chứa file nào.");

                // Upload từng file lên S3
                foreach (var filePath in files)
                {
                    var fileName = Path.GetFileName(filePath);
                    var key = $"{s3Folder}/{fileName}";

                    using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
                    var contentType = filePath.EndsWith(".m3u8") ? "application/x-mpegURL" : "video/mp2t";

                    var request = new PutObjectRequest
                    {
                        BucketName = BucketName,
                        Key = key,
                        InputStream = stream,
                        ContentType = contentType,
                        CannedACL = S3CannedACL.PublicRead
                    };

                    await _s3Client.PutObjectAsync(request);
                }

                // Trả về URL của file master.m3u8 (giả định file chính là master.m3u8)
                return $"https://{BucketName}.s3.amazonaws.com/{s3Folder}/master.m3u8";
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi upload thư mục lên S3: {ex.Message}");
            }
        }

        // Giữ nguyên phương thức cũ để upload file đơn lẻ (poster/backdrop)
        public async Task<string> UploadFileAsync(IFormFile file, string folder)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("File không hợp lệ.");

            var fileName = file.FileName;
            var key = $"{folder}/{fileName}";
            var contentType = file.ContentType;

            using var stream = file.OpenReadStream();
            var request = new PutObjectRequest
            {
                BucketName = BucketName,
                Key = key,
                InputStream = stream,
                ContentType = contentType,
                CannedACL = S3CannedACL.PublicRead
            };

            await _s3Client.PutObjectAsync(request);
            return $"https://{BucketName}.s3.amazonaws.com/{key}";
        }
    }
}