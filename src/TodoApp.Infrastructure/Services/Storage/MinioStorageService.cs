using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel.Args;
using Minio.Exceptions;
using TodoApp.Application.Interfaces.Services.Storage;
using TodoApp.Domain.Settings;

namespace TodoApp.Infrastructure.Services.Storage;

/// <summary>
/// MinIO S3-compatible storage implementation of IFileStorageService
/// </summary>
public class MinioStorageService : IFileStorageService
{
    private readonly IMinioClient _minioClient;
    private readonly MinioSettings _settings;
    private readonly ILogger<MinioStorageService> _logger;

    public MinioStorageService(
        IOptions<MinioSettings> settings,
        ILogger<MinioStorageService> logger)
    {
        _settings = settings.Value;
        _logger = logger;

        // Initialize MinIO client
        _minioClient = new MinioClient()
            .WithEndpoint(_settings.Endpoint)
            .WithCredentials(_settings.AccessKey, _settings.SecretKey)
            .WithSSL(_settings.UseSSL)
            .Build();
    }

    public async Task<string> UploadFileAsync(byte[] fileContent, string fileName, string containerName = "imports")
    {
        try
        {
            var bucketName = containerName ?? _settings.BucketName;

            // Ensure bucket exists
            var bucketExistsArgs = new BucketExistsArgs()
                .WithBucket(bucketName);

            bool found = await _minioClient.BucketExistsAsync(bucketExistsArgs);

            if (!found)
            {
                var makeBucketArgs = new MakeBucketArgs()
                    .WithBucket(bucketName);
                await _minioClient.MakeBucketAsync(makeBucketArgs);
                _logger.LogInformation("Created bucket: {BucketName}", bucketName);
            }

            // Create unique file name with timestamp
            var timestamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
            var uniqueFileName = $"{timestamp}-{fileName}";

            // Upload file
            using var stream = new MemoryStream(fileContent);

            // Determine content type based on file extension
            var contentType = fileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase)
                ? "text/csv"
                : "application/octet-stream";

            var putObjectArgs = new PutObjectArgs()
                .WithBucket(bucketName)
                .WithObject(uniqueFileName)
                .WithStreamData(stream)
                .WithObjectSize(stream.Length)
                .WithContentType(contentType);

            await _minioClient.PutObjectAsync(putObjectArgs);

            _logger.LogInformation("File uploaded to MinIO: {BucketName}/{FileName}", bucketName, uniqueFileName);

            // Return the URL (MinIO format)
            var protocol = _settings.UseSSL ? "https" : "http";
            return $"{protocol}://{_settings.Endpoint}/{bucketName}/{uniqueFileName}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading file to MinIO: {FileName}", fileName);
            throw;
        }
    }

    public async Task<string> DownloadFileAsync(string fileUrl)
    {
        try
        {
            // Parse MinIO URL to extract bucket and object name
            var uri = new Uri(fileUrl);
            var pathSegments = uri.AbsolutePath.TrimStart('/').Split('/');
            var bucketName = pathSegments[0];
            var objectName = string.Join("/", pathSegments.Skip(1));

            // Download object
            using var memoryStream = new MemoryStream();

            var getObjectArgs = new GetObjectArgs()
                .WithBucket(bucketName)
                .WithObject(objectName)
                .WithCallbackStream((stream) =>
                {
                    stream.CopyTo(memoryStream);
                });

            await _minioClient.GetObjectAsync(getObjectArgs);

            memoryStream.Position = 0;
            using var reader = new StreamReader(memoryStream);
            var content = await reader.ReadToEndAsync();

            _logger.LogInformation("File downloaded from MinIO: {FileUrl}", fileUrl);

            return content;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading file from MinIO: {FileUrl}", fileUrl);
            throw;
        }
    }

    public async Task<bool> DeleteFileAsync(string fileUrl)
    {
        try
        {
            var uri = new Uri(fileUrl);
            var pathSegments = uri.AbsolutePath.TrimStart('/').Split('/');
            var bucketName = pathSegments[0];
            var objectName = string.Join("/", pathSegments.Skip(1));

            var removeObjectArgs = new RemoveObjectArgs()
                .WithBucket(bucketName)
                .WithObject(objectName);

            await _minioClient.RemoveObjectAsync(removeObjectArgs);

            _logger.LogInformation("File deleted from MinIO: {FileUrl}", fileUrl);

            return true;
        }
        catch (MinioException ex)
        {
            _logger.LogError(ex, "MinIO error deleting file: {FileUrl}", fileUrl);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting file from MinIO: {FileUrl}", fileUrl);
            return false;
        }
    }

    public async Task<bool> FileExistsAsync(string fileUrl)
    {
        try
        {
            var uri = new Uri(fileUrl);
            var pathSegments = uri.AbsolutePath.TrimStart('/').Split('/');
            var bucketName = pathSegments[0];
            var objectName = string.Join("/", pathSegments.Skip(1));

            var statObjectArgs = new StatObjectArgs()
                .WithBucket(bucketName)
                .WithObject(objectName);

            await _minioClient.StatObjectAsync(statObjectArgs);

            return true;
        }
        catch (ObjectNotFoundException)
        {
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking file existence in MinIO: {FileUrl}", fileUrl);
            return false;
        }
    }
}
