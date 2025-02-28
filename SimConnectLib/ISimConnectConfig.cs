namespace CFIT.SimConnectLib
{
    public interface ISimConnectConfig
    {
        public string ClientName { get; }

        public int RetryDelay { get; }
        public int StaleTimeout { get; }
        public int CheckInterval { get; }

        public bool CreateWindow { get; }
        public int MsgSimConnect { get; }
        public int MsgConnectRequest { get; }

        public uint IdBase { get; }
        public uint SizeVariables { get; }
        public uint SizeEvents { get; }
        public uint SizeSimStates { get { return 100; } }
        public uint SizeInputEvents { get; }

        public bool VerboseLogging { get; }

        public string BinaryMsfs2020 { get { return "FlightSimulator"; } }
        public string BinaryMsfs2024 { get { return "FlightSimulator2024"; } }
    }
}
