using FTM.Common.Entities;
using FTMCommon.Common.MainService;
using FTMPlus.Common.MainService;
using FTMPlus.Entities;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace FTMPlus.Common.Helper
{
    public abstract class BaseFileAdaptor : IFileProccesor, ISearchAdaptor
    {
        public Flow Flow { get; private set; } = null!;
        public IServiceScope ServiceScope { get; private set; } = null!;
        public int Order { get; private set; } = 1;
        public FlowProcess Process { get { return this.Flow.Procces.FirstOrDefault(t => t.Order == this.Order)!; } }
        public IEnumerable<FileMeta>? ProcessSourceFiles { get { return this.Process.SourceFiles?.Where(t => t.Status == ProcessingStatus.New); } }

        public async Task Set(IServiceScope ctx, Flow flow, int order = 1,bool isSource=false)
        {
            this.Flow = flow;
            this.Order = order == 0 ? 1 : order;
            this.ServiceScope = ctx;
            if (order == 1 && isSource)
            {
                await this.CheckLockFiles();
            }
            //if (this.Process == null)
            //{
            //    throw new Exception("Proccess not found");
            //}
        }



        public static string ConvertPatternToRegex(string pattern)
        {
            var regex = "^" + Regex.Escape(pattern).Replace("\\*", ".*").Replace("\\?", ".") + "$";
            return regex;
        }


        public static bool IsMatch(string value, string pattern)
        {
            string path = Path.GetFileName(value);
            var snc = Regex.IsMatch(path, ConvertPatternToRegex(pattern));
            return snc;
        }

        public bool isOnlyRedis
        {
            get { return this.Process.SourceResultAction == "D" || this.Process.SourceResultAction == "R"; }
        }

        private async Task CheckLockFiles()
        {
            var lockService = this.ServiceScope.ServiceProvider.GetRequiredService<DistributedLockService>();

            foreach (var item in this.Process.SourceFiles)
            {
                var value = await lockService.GetExistingLockValueAsync(item.lockKey);
                if (value == LockValue.Start)
                {
                    item.Status = ProcessingStatus.New;
                    await lockService.AcquireLockAsync(item.lockKey, 600, LockValue.Processing, isOnlyRedis, true);
                }
                else
                {
                    item.Status = ProcessingStatus.Processing;
                }
            }

        }

        public string GetExpresionAsync(string sourceDir, object forInData = null)
        {
            return this.ServiceScope.ServiceProvider.GetRequiredService<ExpressionServices>().GetExpression<string>(sourceDir, this.Flow, forInData,sourceDir);
        }

        //public abstract   Task DoProccessAsync();

        public abstract Task CopyFilesAsync(FileServer sourceServer, FileServer targetServer, BaseFileAdaptor target);
        //public abstract Task CheckAsync();
        public virtual Task ConnectAsync(FileServer server)
        {
            return Task.CompletedTask;
        }
        //public abstract Task CopyFilesAsync();
        public abstract Task<List<FileMeta>> FindFilesAsync(FileServer fileServer, FlowProcess procces,bool autoClose=false);
        public virtual Task Disconnect()
        {
            return Task.CompletedTask;
        }
        public string ConvertToPlatformIndependentPath(string path)
        {
            return path.Replace(Path.DirectorySeparatorChar, '/');
        }

        /// <summary>
        /// belitilen yerden dosyayi yukler
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public abstract Task UploadAsync(FileServer fileServer, string path, Stream content, bool append);

        public abstract Task AfterCopyAsync();
    }
}
