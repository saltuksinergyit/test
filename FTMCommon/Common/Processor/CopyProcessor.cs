using FTM.Common.Entities;
using FTMPlus.Common.Helper;
using FTMPlus.Common.MainService;
using FTMPlus.Entities;
using Org.BouncyCastle.Asn1.Ocsp;

namespace FTMPlus.Common.Processor
{
    /// <summary>
    /// Dosyalari Kopyalar a dan b ye
    /// </summary>
    public class CopyProcessor : BaseProccessor
    {

        public override async Task DoProccessAsync()
        {
            var source = this.GetServerAsync(this.Process.SourceServerCode);
            var target = this.GetServerAsync(this.Process.TargetServerCode);


            if (source == null || target == null)
            {
                throw new Exception("Server not found");
            }

            var sourceAdaptor = this.GetFileAdaptor(source.ServerType);
            var targetAdaptor = this.GetFileAdaptor(target.ServerType);

            if (sourceAdaptor == null || targetAdaptor == null)
            {
                throw new Exception("Adaptor not found");
            }

            await sourceAdaptor.Set(this.ServiceScope, this.Flow, this.Order, true);
            if (this.Order > 1 && (this.Process.SourceFiles == null || this.Process.SourceFiles.Count == 0))
            {
                this.Process.SourceFiles = await sourceAdaptor.FindFilesAsync(source, this.Process);
            }
            await targetAdaptor.Set(this.ServiceScope, this.Flow, this.Order);
            await CopyProcessorAsync(source, target, sourceAdaptor, targetAdaptor);
            Task.WaitAll(sourceAdaptor.AfterCopyAsync(), SetLockFiles(sourceAdaptor, LockValue.Processed));
        }

        private async Task SetLockFiles(BaseFileAdaptor sourceAdaptor, LockValue processed)
        {
            if (this.Order == 1)
            {
                var lockService = this.ServiceScope.ServiceProvider.GetRequiredService<DistributedLockService>();

                foreach (var item in this.Process.SourceFiles.Where(x => x.Status == ProcessingStatus.Processed))
                {
                    if (sourceAdaptor.isOnlyRedis)
                    {
                        await lockService.ReleaseLockAsync(item.lockKey, sourceAdaptor.isOnlyRedis);
                    }
                    else
                    {
                        await lockService.AcquireLockAsync(item.lockKey, 100, processed, false, true);
                    }

                }
            }

        }

        private async Task CopyProcessorAsync(FileServer source, FileServer target, BaseFileAdaptor sourceAdaptor, BaseFileAdaptor targetAdaptor)
        {
            await sourceAdaptor.CopyFilesAsync(source, target, targetAdaptor);
        }

        private BaseFileAdaptor GetFileAdaptor(ServerType serverType)
        {
            switch (serverType)
            {
                case ServerType.Filesystem:
                    return this.ServiceScope.ServiceProvider.GetRequiredService<FileAdaptor>();
                case ServerType.FTP:
                    return this.ServiceScope.ServiceProvider.GetRequiredService<FTPAdaptor>();
                case ServerType.SFTP:
                    return this.ServiceScope.ServiceProvider.GetRequiredService<SFTPAdaptor>();
                case ServerType.S3:
                    return this.ServiceScope.ServiceProvider.GetRequiredService<S3Adaptor>();
                    //case ServerType.Mail:
                    //    break;
                    //default:
                    //    break;
            }

            return null;
        }
    }
}
