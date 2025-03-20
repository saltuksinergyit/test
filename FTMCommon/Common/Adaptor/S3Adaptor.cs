using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using FTM.Common.Entities;
using FTMPlus.Common.Helper;
using FTMPlus.Entities;
using Irony.Parsing;
using System.IO;

namespace FTMPlus.Common.Processor
{
    public class S3Adaptor : BaseFileAdaptor
    {
        private const long PartSize = 5 * 1024 * 1024; // 5MB parçalar

        protected AmazonS3Client AmazonS3Client { get; set; } = default!;
        public FileServer SourceServer { get; private set; } = default!;

        public override async Task ConnectAsync(FileServer sourceServer)
        {
            if (sourceServer == null)
            {
                throw new Exception("Server not found");
            }
            if (sourceServer.ServerType != ServerType.S3)
            {
                throw new Exception("Server type not supported");
            }
            if (this.AmazonS3Client != null)
            {
                return;
            }
            this.AmazonS3Client = new AmazonS3Client(sourceServer.ServerUser, sourceServer.ServerPassword, new AmazonS3Config { ServiceURL = sourceServer.ServerIp, ForcePathStyle = true });
            this.SourceServer = sourceServer;
            await Task.CompletedTask;
        }

        public override Task Disconnect()
        {
            this.AmazonS3Client?.Dispose();
            return Task.CompletedTask;
        }

        public override async Task AfterCopyAsync()
        {
            var source = this.Process.SourceFiles.Where(t => t.Status == ProcessingStatus.Processed);
            if (this.Process.SourceResultAction == "D")
            {

                foreach (var item in source)
                {
                    item.FullPath = this.S3DirClear(item.FullPath);

                    var deleteRequest = new DeleteObjectRequest
                    {
                        BucketName = SourceServer.ServerName,
                        Key = item.FullPath
                    };

                    await AmazonS3Client.DeleteObjectAsync(deleteRequest);
                }
            }
            if (this.Process.SourceResultAction == "R")
            {
                string ext = this.Process.SourceNewFileExtension;


                foreach (var item in source)
                {

                    item.FullPath = this.S3DirClear(item.FullPath);
                    string newKey = item.FullPath + ext;

                    var copyRequest = new CopyObjectRequest
                    {
                        SourceBucket = SourceServer.ServerName,
                        SourceKey = item.FullPath,
                        DestinationBucket = SourceServer.ServerName,
                        DestinationKey = newKey
                    };

                    await AmazonS3Client.CopyObjectAsync(copyRequest);

                    // Eski dosyayı sil
                    var deleteRequest = new DeleteObjectRequest
                    {
                        BucketName = SourceServer.ServerName,
                        Key = item.FullPath
                    };

                    await AmazonS3Client.DeleteObjectAsync(deleteRequest);
                }
            }
        }


        public override async Task CopyFilesAsync(FileServer sourceServer, FileServer targetServer, BaseFileAdaptor target)
        {

            for (int i = 0; i < this.ProcessSourceFiles?.Count(); i++)
            {
                try
                {
                    var item = this.ProcessSourceFiles.ElementAt(i);
                    item.FullPath = this.S3DirClear(item.FullPath);

                    var targetFileFullPath = this.GetExpresionAsync(this.Process.TargetDirectoryName, new { index = i, item = item });
                    var targetFilePatern = this.GetExpresionAsync(this.Process.TargetFileName, new { index = i, item = item });
                    item.DestinationPath = Path.Combine(targetFileFullPath, targetFilePatern);
                    var request = new GetObjectRequest
                    {
                        BucketName = sourceServer.ServerName,
                        Key = item.FullPath
                    };
                    //using var memoryStream = new MemoryStream();
                    //await response.ResponseStream.CopyToAsync(memoryStream);
                    //if (sourceServer.ServerType == targetServer.ServerType && target is S3Adaptor)
                    //{
                    //    var metadata = await AmazonS3Client.GetObjectMetadataAsync(new GetObjectMetadataRequest
                    //    {
                    //        BucketName = sourceServer.ServerName,
                    //        Key = item.FullPath
                    //    });

                    //    using var response = await AmazonS3Client.GetObjectAsync(request);
                    //   await  (target as S3Adaptor).UploadS3ToS3Async(targetServer, item.DestinationPath, response, metadata);
                    //}else
                    //{

                    //    using var response = await AmazonS3Client.GetObjectAsync(request);
                    //    await target.UploadAsync(targetServer, item.DestinationPath, response.ResponseStream, true);
                    //}


                    using var response = await AmazonS3Client.GetObjectAsync(request);
                    await target.UploadAsync(targetServer, item.DestinationPath, response.ResponseStream, true);
                    item.Status = ProcessingStatus.Processed; // Başarıyla işlendi
                }
                catch (Exception ex)
                {
                    // Hata yönetimi: Logla veya özel bir işlem yap
                }
            }
        }

        private async Task  UploadS3ToS3Async(FileServer targetServer, string destinationPath, GetObjectResponse response, GetObjectMetadataResponse metadata)
        {
            await this.ConnectAsync(targetServer);

            try
            {
                long fileSize = metadata.ContentLength;
                destinationPath = this.S3DirClear(destinationPath);

                // 2️⃣ **Dosya küçükse direkt yükle, büyükse parçalı yükleme yap**
                if (fileSize <= PartSize || true)
                {
                    // 1. Kaynak dosyanın bilgilerini al
                    var getObjectResponse = response;
                    using var sourceStream = getObjectResponse.ResponseStream;
                    using var memoryStream = new MemoryStream();
                    // 2. Kaynak dosyayı belleğe al
                    await sourceStream.CopyToAsync(memoryStream);

                    // **Dosyanın boş olup olmadığını kontrol et**
                    if (memoryStream.Length == 0)
                    {
                        throw new Exception("Kaynak dosya boş!");
                    }

                    // 3. Hedef bucket’a yükle
                    var putRequest = new PutObjectRequest
                    {
                        BucketName = targetServer.ServerName,
                        Key = destinationPath,
                        InputStream = memoryStream, // Bellekten yükleme
                        ContentType = getObjectResponse.Headers.ContentType // İçeriğin tipini koru
                    };

                    await AmazonS3Client.PutObjectAsync(putRequest);
                }
                else
                {
                    var initiateResponse = await AmazonS3Client.InitiateMultipartUploadAsync(new InitiateMultipartUploadRequest
                    {
                        BucketName = targetServer.ServerName,
                        Key = destinationPath,
                    });

                    string uploadId = initiateResponse.UploadId;
                    var partETags = new List<PartETag>();
                    int partNumber = 1;
                    long uploadedSize = 0;

                    byte[] buffer = new byte[PartSize];
                    int bytesRead;

                    while ((bytesRead = await response.ResponseStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                    {
                        uploadedSize += bytesRead;
                        using var memoryStream = new MemoryStream(buffer, 0, bytesRead);

                        

                        var uploadPartResponse = await AmazonS3Client.UploadPartAsync(new UploadPartRequest
                        {
                            BucketName = targetServer.ServerName,
                            Key = destinationPath,
                            UploadId = uploadId,
                            PartNumber = partNumber,
                            InputStream = memoryStream
                        });

                        partETags.Add(new PartETag { PartNumber = partNumber, ETag = uploadPartResponse.ETag });
                        partNumber++;
                        Console.WriteLine($"📦 {partNumber - 1}. parça yüklendi...");
                    }

                    await AmazonS3Client.CompleteMultipartUploadAsync(new CompleteMultipartUploadRequest
                    {
                        BucketName = targetServer.ServerName,
                        Key = destinationPath,
                        UploadId = uploadId,
                        PartETags = partETags
                    }); 

                }

            }
            catch (Exception ex)
            {
                 
            }
        

        }

        public override async Task<List<FileMeta>> FindFilesAsync(FileServer fileServer, FlowProcess procces, bool autoClose = false)
        {
            try
            {
                await this.ConnectAsync(fileServer);
                var sourceServer = fileServer;

                string path = this.S3DirClear(Path.Combine(this.GetExpresionAsync(procces.SourceDirectoryName)));

                string patern = this.GetExpresionAsync(procces.SourceFileName);
                var obj = await AmazonS3Client.ListObjectsV2Async(new ListObjectsV2Request { BucketName = sourceServer.ServerName, Prefix = path });
                var fileMetas = obj.S3Objects.Where(x => IsMatch(x.Key, patern)).Select(x => new FileMeta
                {
                    CreatedDate = x.LastModified,
                    FileName = Path.GetFileName(x.Key),
                    FullPath = x.Key,
                    Size = x.Size,
                    SourceType = SourceType.S3,
                    Status = ProcessingStatus.New,
                    OrginalData = x
                }).ToList();
                return fileMetas.OrderBy(x => x.CreatedDate).ToList();

            }
            finally
            {


            }
        }

        public override async Task UploadAsync(FileServer fileServer, string path, Stream content, bool append)
        {
                await this.ConnectAsync(fileServer);
            var transferUtility = new TransferUtility(AmazonS3Client);

            try
            {
                path = this.S3DirClear(path);





                // TransferUtilityUploadRequest nesnesi oluşturuluyor.
                var uploadRequest = new TransferUtilityUploadRequest
                {
                    InputStream = content,     // Yüklenecek verinin bulunduğu stream
                    BucketName = fileServer.ServerName,
                    Key = path,
                    PartSize = 5 * 1024 * 1024      // Parça boyutu: 5 MB
                };

                // Multipart upload otomatik olarak işlenir.
                await transferUtility.UploadAsync(uploadRequest);
      
                return;

                byte[] buffer = new byte[5 * 1024 * 1024];
            





                if (content.Length <= buffer.Length)
                {

                    var putRequest = new PutObjectRequest
                    {
                        BucketName = fileServer.ServerName,
                        Key = path,
                        InputStream = content, // Bellekten yükleme 
                    };

                    await this.AmazonS3Client.PutObjectAsync(putRequest);


                    return;
                }

                var initRequest = new InitiateMultipartUploadRequest
                {
                    BucketName = fileServer.ServerName,
                    Key = path
                };


                var initResponse = await AmazonS3Client.InitiateMultipartUploadAsync(initRequest);
                var uploadId = initResponse.UploadId;

                var partETags = new List<PartETag>();
                int partNumber = 1;
                int bytesRead;

                while ((bytesRead = await content.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    using var chunkStream = new MemoryStream(buffer, 0, bytesRead);
                    var uploadPartRequest = new UploadPartRequest
                    {
                        BucketName = fileServer.ServerName,
                        Key = path,
                        UploadId = uploadId,
                        PartNumber = partNumber,
                        InputStream = chunkStream
                    };

                    var uploadPartResponse = await AmazonS3Client.UploadPartAsync(uploadPartRequest);
                    partETags.Add(new PartETag(partNumber, uploadPartResponse.ETag));
                    partNumber++;
                }

                var completeRequest = new CompleteMultipartUploadRequest
                {
                    BucketName = fileServer.ServerName,
                    Key = path,
                    UploadId = uploadId,
                    PartETags = partETags
                };

                await AmazonS3Client.CompleteMultipartUploadAsync(completeRequest);




            }
            catch (Exception ex)
            {

                //throw;
            }
            finally
            {
                transferUtility.Dispose();
            }
        }

        private string S3DirClear(string path)
        {
            path = this.ConvertToPlatformIndependentPath(path);
            if (path.StartsWith("/"))
            {
                path = path.Substring(1);
            }
            return path;
        }
    }
}
