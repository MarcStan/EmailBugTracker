using System.Threading.Tasks;

namespace EmailBugTracker.Logic
{
    public interface IWorkItemProcessor
    {
        Task ProcessWorkItemAsync(WorkItem workItem);
    }
}
