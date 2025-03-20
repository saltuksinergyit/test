using FTMPlus.Entities;

namespace FTMCommon.Common.MainService
{
    public class FlowService
    {
        public Flow Flow { get; protected set; } = default!;  

        public FlowProcess Process { get { return Flow.Procces.FirstOrDefault(x => x.Order == Flow.ProccessOrder)!; } } 

        public void Set(Flow flow)
        {
            this.Flow = flow;
        }   


    }
}
