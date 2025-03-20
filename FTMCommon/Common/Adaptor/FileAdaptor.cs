using FTM.Common.Entities;
using FTMPlus.Common.Helper;
using FTMPlus.Entities;

namespace FTMPlus.Common.Processor
{
    public class FileAdaptor : BaseFileAdaptor
    {
        /// <summary>
        /// kopyalama islemi bittikten sonra yapilacak islemler
        /// </summary>
        /// <returns></returns>
        public override Task AfterCopyAsync()
        {
            var source = this.Process.SourceFiles.Where(t => t.Status == ProcessingStatus.Processed);
            if (this.Process.SourceResultAction == "D")
            {
                foreach (var item in source)
                {

                    if (File.Exists(item.FullPath))
                    {
                        File.Delete(item.FullPath);
                    }
                }
            }
            if (this.Process.SourceResultAction == "R")
            {
                string ext = this.Process.SourceNewFileExtension;
                foreach (var item in source)
                {
                    File.Move(item.FullPath, item.FullPath + ext);
                }
            }
            return Task.CompletedTask;
        }
        /// <summary>
        /// dosyalari kopyalar
        /// </summary>
        /// <param name="sourceServer">kaynak server</param>
        /// <param name="targetServer">hedef server</param>
        /// <param name="target">hedef adaptor</param>
        /// <returns></returns>
        public override async Task CopyFilesAsync(FileServer sourceServer, FileServer targetServer, BaseFileAdaptor target)
        {
            // ikisi de file ayni direkt koplaya
            /**
             * file system native oldugu icin direk kopyalama yapilabilir
             */
            if (targetServer.ServerType == ServerType.Filesystem)
            {
                for (int i = 0; i < this.ProcessSourceFiles?.Count(); i++)
                {
                    try
                    {
                        var item = this.ProcessSourceFiles.ElementAt(i);
                        var targetFileFullPath = this.GetExpresionAsync(this.Process.TargetDirectoryName, new { index = i, item = item });
                        var targetFilePatern = this.GetExpresionAsync(this.Process.TargetFileName, new { index = i, item = item });
                        item.DestinationPath = Path.Combine(targetFileFullPath, targetFilePatern);
                        if (!Directory.Exists(targetFileFullPath))
                        {
                            Directory.CreateDirectory(targetFileFullPath);
                        }
                        else if (File.Exists(item.DestinationPath))
                        {
                            File.Delete(item.DestinationPath);
                        }
                        //kok dizinler ayni ise move etmek IO icin daha iyidir.
                        if (this.Process.SourceResultAction == "D" && Path.GetPathRoot(item.FullPath) == Path.GetPathRoot(item.DestinationPath))
                        {
                            File.Move(item.FullPath, item.DestinationPath);
                        }
                        else
                            File.Copy(item.FullPath, item.DestinationPath);
                        item.Status = ProcessingStatus.Processed;
                    }
                    catch (Exception ex)
                    {

                        //hata alirsa yapilacaklar islemkesme vb.
                    }

                }
            }
            else
            {
                for (int i = 0; i < this.ProcessSourceFiles?.Count(); i++)
                {
                    try
                    {
                        var item = this.ProcessSourceFiles.ElementAt(i);
                        var targetFileFullPath = this.GetExpresionAsync(this.Process.TargetDirectoryName, new { index = i, item = item });
                        var targetFilePatern = this.GetExpresionAsync(this.Process.TargetFileName, new { index = i, item = item });
                        item.DestinationPath = Path.Combine(targetFileFullPath, targetFilePatern);
                        using (var fl = new FileStream(item.FullPath, FileMode.Open))
                        {
                            await target.UploadAsync(targetServer,item.DestinationPath, fl, true);
                        }
                        item.Status = ProcessingStatus.Processed;
                    }
                    catch (Exception ex)
                    {

                        //hata alirsa yapilacaklar islemkesme vb.
                    }

                }
                await target.Disconnect();
            }

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
                string path = this.GetExpresionAsync(procces.SourceDirectoryName);
                string patern = this.GetExpresionAsync(procces.SourceFileName);
                var files = Directory.GetFiles(path, patern);
                if (files.Length == 0)
                {
                    return new List<FileMeta>();
                }
                else
                {
                    List<FileMeta> fileMetas = new List<FileMeta>();
                    foreach (var item in files)
                    {
                        FileInfo fileInfo = new FileInfo(item);
                        fileMetas.Add(new FileMeta()
                        {
                            CreatedDate = fileInfo.CreationTime,
                            UpdateDate = fileInfo.LastWriteTime,
                            FileName = fileInfo.Name,
                            FullPath = fileInfo.FullName,
                            Size = fileInfo.Length,
                            SourceType = SourceType.Filesystem,
                            Status = ProcessingStatus.New,
                            ServerCode = fileServer.ServerCode
                        });
                    }
                    return fileMetas.OrderBy(x => x.CreatedDate).ToList();
                }
            }
            finally
            {
                await Task.CompletedTask;
            }

        }
        /// <summary>
        /// dosyalari yukler
        /// </summary>
        /// <returns></returns>
        public override Task UploadAsync(FileServer fileServer, string path, Stream content, bool append)
        {
            //dosyalari yukleme islemi
            string dir = Path.GetDirectoryName(path)!;
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            using (var fs = new FileStream(path, append ? FileMode.Append : FileMode.Create))
            {
                content.CopyTo(fs);
            }
            return Task.CompletedTask;
        }
    }
}
