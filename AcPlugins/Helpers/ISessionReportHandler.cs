using AcTools.ServerPlugin.DynamicConditions.AcPlugins.Info;

namespace AcTools.ServerPlugin.DynamicConditions.AcPlugins.Helpers {
    public interface ISessionReportHandler {
        void HandleReport(SessionInfo report);
    }
}