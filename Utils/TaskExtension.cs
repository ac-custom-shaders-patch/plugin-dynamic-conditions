using System.Threading.Tasks;

namespace AcTools.ServerPlugin.DynamicConditions.Utils {
    public static class TaskExtension {
        public static void Ignore(this Task task) {
            task.ContinueWith(x => {
                Logging.Write(x.Exception?.Flatten());
            }, TaskContinuationOptions.NotOnRanToCompletion);
        }
    }
}