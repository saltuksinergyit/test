using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using FluentFTP;
using FTM.Common.Entities;
using FTMPlus.Common.Helper;
using FTMPlus.Entities;

namespace FTMPlus.Common.Processor
{
    public class FTPAdaptor : BaseFileAdaptor
    {
        protected FtpClient FtpClient { get; set; } = default!;

        public override async Task AfterCopyAsync()
        {
            var source = this.Process.SourceFiles.Where(t => t.Status == ProcessingStatus.Processed);

            if (this.Process.SourceResultAction == "D")
            {
                foreach (var item in source)
                {
                    item.FullPath = this.ConvertToPlatformIndependentPath(item.FullPath);
                    if (FtpClient.FileExists(item.FullPath))
                    {
                        FtpClient.DeleteFile(item.FullPath);
                    }
                }
            }
            else if (this.Process.SourceResultAction == "R")
            {
                string ext = this.Process.SourceNewFileExtension;

                foreach (var item in source)
                {
                    item.FullPath = this.ConvertToPlatformIndependentPath(item.FullPath);
                    if (FtpClient.FileExists(item.FullPath))
                    {
                        FtpClient.MoveFile(item.FullPath, item.FullPath + ext);
                    }
                }
            }
            await Task.CompletedTask;
        }

        public override async Task CopyFilesAsync(FileServer sourceServer, FileServer targetServer, BaseFileAdaptor target)
        {
            await this.ConnectAsync(sourceServer);
            for (int i = 0; i < this.ProcessSourceFiles?.Count(); i++)
            {
                try
                {
                    var item = this.ProcessSourceFiles.ElementAt(i);
                    var targetFileFullPath = this.GetExpresionAsync(this.Process.TargetDirectoryName, new { index = i, item = item });
                    var targetFilePatern = this.GetExpresionAsync(this.Process.TargetFileName, new { index = i, item = item });
                    item.DestinationPath = Path.Combine(targetFileFullPath, targetFilePatern);

                    using var fileStream = FtpClient.OpenRead(item.FullPath);
                    await target.UploadAsync(targetServer, item.DestinationPath, fileStream, true);

                    item.Status = ProcessingStatus.Processed;
                }
                catch (Exception ex)
                {
                    // Hata işleme mekanizması burada olmalı
                }
            }

            await target.Disconnect();
        }

        public override async Task<List<FileMeta>> FindFilesAsync(FileServer fileServer, FlowProcess process, bool autoClose = false)
        {
            try
            {
                await this.ConnectAsync(fileServer);
                string path = this.GetExpresionAsync(process.SourceDirectoryName);
                string pattern = this.GetExpresionAsync(process.SourceFileName);

                var fileList = FtpClient.GetListing(path);
                var fileMetas = fileList
                    .Where(x => x.Type == FtpObjectType.File && IsMatch(x.Name, pattern))
                    .Select(x => new FileMeta
                    {
                        CreatedDate = x.Modified,
                        FileName = x.Name,
                        FullPath = x.FullName,
                        Size = x.Size,
                        SourceType = SourceType.FTP,
                        Status = ProcessingStatus.New,
                        ServerCode = fileServer.ServerCode
                    })
                    .OrderBy(x => x.CreatedDate)
                    .ToList();

                return fileMetas;
            }
            finally
            {
                if (autoClose)
                    await this.Disconnect();
            }
        }

        public override async Task UploadAsync(FileServer fileServer, string path, Stream content, bool append)
        {
            try
            {
                path = this.ConvertToPlatformIndependentPath(path);
                await this.ConnectAsync(fileServer);
                string dir = Path.GetDirectoryName(path)!;

                if (!FtpClient.FileExists(dir))
                {
                    FtpClient.CreateDirectory(dir);
                }

                if (FtpClient.FileExists(path))
                {
                    FtpClient.DeleteFile(path);
                    append = false;
                }

                // **Dosyayı parça parça yükleme (5MB)**
                byte[] buffer = new byte[5 * 1024 * 1024]; // 5MB parçalar
                int bytesRead;

                using var ftpStream = FtpClient.OpenWrite(path);
                while ((bytesRead = await content.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    await ftpStream.WriteAsync(buffer, 0, bytesRead);
                }
                await ftpStream.FlushAsync();
                await ftpStream.DisposeAsync();
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public override async Task ConnectAsync(FileServer sourceServer)
        {
            if (sourceServer == null)
            {
                throw new Exception("Server not found");
            }
            if (sourceServer.ServerType != ServerType.FTP)
            {
                throw new Exception("Server type not supported");
            }
            if (this.FtpClient != null && this.FtpClient.IsConnected)
            {
                return;
            }

            this.FtpClient = new FtpClient(sourceServer.ServerIp, sourceServer.ServerUser, sourceServer.ServerPassword)
            {
                Port = sourceServer.ServerPort,
                Config = new FtpConfig
                {
                    DataConnectionType = FtpDataConnectionType.AutoPassive, // Use passive mode
                    ConnectTimeout = 10000, // Connection timeout (10 seconds)
                    ReadTimeout = 10000,
                    EncryptionMode= FtpEncryptionMode.Explicit
                } 
            };

            this.FtpClient.ValidateCertificate += new FtpSslValidation((control, e) =>
            {
                e.Accept = true; // Accept all certificates (for testing purposes, consider proper validation in production)
            });

            this.FtpClient.Connect();
            await Task.CompletedTask;
        }

        public override Task Disconnect()
        {
            this.FtpClient.Disconnect();
            return Task.CompletedTask;
        }
    }
}
