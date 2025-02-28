namespace CFIT.SimConnectLib.Modules.MobiFlight
{
    public interface IMobiConfig
    {
        public string ClientName { get; }
        public int MobiRetryDelay { get; }
        public bool MobiWriteLvars { get; }
        public string MobiLvarFile { get; }
        public bool MobiSetVarPerFrame { get; }
        public int MobiVarsPerFrame { get; }
        public uint MobiSizeVariables { get; }
    }
}
