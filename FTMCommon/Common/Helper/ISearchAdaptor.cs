using FTM.Common.Entities;
using FTMPlus.Entities;

namespace FTMPlus.Common.Helper
{
    public interface ISearchAdaptor
    {
        public Task Set(IServiceScope ctx, Flow flow, int order = 1, bool isSource = false);
        public abstract Task<List<FileMeta>> FindFilesAsync(FileServer fileServer, FlowProcess procces,bool autoClose=false);


        public  Task Disconnect();
        public Task ConnectAsync(FileServer server);

    }
}
