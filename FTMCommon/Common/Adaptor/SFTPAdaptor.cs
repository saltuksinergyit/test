using FTM.Common.Entities;
using FTMPlus.Common.Helper;
using FTMPlus.Entities;
using Renci.SshNet;

namespace FTMPlus.Common.Processor
{
    public class SFTPAdaptor : BaseFileAdaptor
    { 
        protected SftpClient SftpClient { get; set; } = default!; 
        public override async Task AfterCopyAsync()
        {
            var source = this.Process.SourceFiles.Where(t => t.Status == ProcessingStatus.Processed);
            if (this.Process.SourceResultAction == "D")
            {

                foreach (var item in source)
                {
                    item.FullPath = this.ConvertToPlatformIndependentPath(item.FullPath);
                    if (SftpClient.Exists(item.FullPath))
                    {
                        await SftpClient.DeleteFileAsync(item.FullPath,CancellationToken.None);
                    }
                }
            }
            if (this.Process.SourceResultAction == "R")
            {
                string ext = this.Process.SourceNewFileExtension;


                foreach (var item in source)
                {
                    item.FullPath = this.ConvertToPlatformIndependentPath(item.FullPath);
                    if (SftpClient.Exists(item.FullPath))
                    {
                        SftpClient.RenameFile(item.FullPath, item.FullPath + ext);
                    }
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
                    var targetFileFullPath = this.GetExpresionAsync(this.Process.TargetDirectoryName, new { index = i, item = item });
                    var targetFilePatern = this.GetExpresionAsync(this.Process.TargetFileName, new { index = i, item = item });
                    item.DestinationPath = Path.Combine(targetFileFullPath, targetFilePatern);
                    await target.UploadAsync(targetServer, item.DestinationPath, SftpClient.OpenRead(item.FullPath), true);
                    item.Status = ProcessingStatus.Processed;
                }
                catch (Exception ex)
                {

                    //hata alirsa yapilacaklar islemkesme vb.
                }

            }

            await target.Disconnect();
        }

        /// <summary>
        /// dosyalari bulur
        /// </summary>
        /// <param name="fileServer"></param>
        /// <param name="procces"></param>
        /// <returns></returns>
        public override async Task<List<FileMeta>> FindFilesAsync(FileServer fileServer, FlowProcess procces, bool autoClose = false)
        {
            try
            {
                await this.ConnectAsync(fileServer);
                string path = this.GetExpresionAsync(procces.SourceDirectoryName);
                string patern = this.GetExpresionAsync(procces.SourceFileName);
                var obj = this.SftpClient.ListDirectory(path).ToList();
                var fileMetas = obj.AsQueryable().Where(x => IsMatch(x.Name, patern)).Select(x => new FileMeta
                {
                    CreatedDate = x.LastWriteTime,
                    FileName = x.Name,
                    FullPath = x.FullName,
                    Size = x.Length,
                    SourceType = SourceType.SFTP,
                    Status = ProcessingStatus.New,
                    ServerCode = fileServer.ServerCode
                }).ToList();
                return fileMetas.OrderBy(x => x.CreatedDate).ToList();
            }
            finally
            {
                if (autoClose)
                    await this.Disconnect();
            }

        }
        /// <summary>
        /// belitilen yerden dosyayi yukler
        /// </summary>
        /// <param name="path"></param>
        /// <param name="content"></param>
        /// <param name="append"></param>
        /// <returns></returns>
        public override async Task UploadAsync(FileServer fileServer, string path, Stream content, bool append)
        {
            try
            {
                path = this.ConvertToPlatformIndependentPath(path);
                await this.ConnectAsync(fileServer);
                string dir = this.ConvertToPlatformIndependentPath(Path.GetDirectoryName(path)!);
                if (!this.SftpClient.Exists(dir))
                {
                    this.SftpClient.CreateDirectory(dir);
                }
                if (this.SftpClient.Exists(path))
                {
                    await this.SftpClient.DeleteAsync(path);
                    append = false;

                } 

                using var sftpStream = SftpClient.Open(path, FileMode.Create);
                byte[] buffer = new byte[5 * 1024 * 1024]; // 5MB bloklar
                int bytesRead;
                while ((bytesRead = await content.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    sftpStream.Write(buffer, 0, bytesRead);
                }
                sftpStream.Flush();
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
            if (sourceServer.ServerType != ServerType.SFTP)
            {
                throw new Exception("Server type not supported");
            }
            if (this.SftpClient != null && this.SftpClient.IsConnected)
            {
                return;
            }
            int port = sourceServer.ServerPort;
            this.SftpClient = new SftpClient(sourceServer.ServerIp, port, sourceServer.ServerUser, sourceServer.ServerPassword);
            await this.SftpClient.ConnectAsync(CancellationToken.None);
        }
        public override Task Disconnect()
        {
            this.SftpClient.Disconnect();
            return Task.CompletedTask;
        } 
    }
}
