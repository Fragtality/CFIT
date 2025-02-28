using CFIT.SimConnectLib.Definitions;
using System.Collections.Generic;

namespace CFIT.SimConnectLib.SimStates
{
    public enum SimStateUpdate
    {
        BOTH = 0,
        SUBSCRIBE,
        POLL
    }

    public enum SimStateData
    {
        INT = 0,
        FLOAT,
        STRING,
        OBJECT
    }

    public class SimStateInfo(MappedID id, string name, SimStateUpdate update, SimStateData data)
    {
        public virtual MappedID EventId { get; set; } = id;
        public virtual string Name { get; set; } = name;
        public virtual SimStateUpdate UpdateType { get; set; } = update;
        public virtual SimStateData DataType { get; set; } = data;

        public static Dictionary<string, SimStateInfo> CreateStateInfo(MappedIdStore idStore)
        {
            var dict = new Dictionary<string, SimStateInfo>
            {
                { "AircraftLoaded", new(idStore.MapConstant("AircraftLoaded"), "AircraftLoaded", SimStateUpdate.BOTH, SimStateData.STRING) },
                { "DialogMode", new(idStore.MapConstant("DialogMode"), "DialogMode", SimStateUpdate.POLL, SimStateData.INT) },
                { "FlightLoaded", new(idStore.MapConstant("FlightLoaded"), "FlightLoaded", SimStateUpdate.BOTH, SimStateData.STRING) },
                { "FlightPlan", new(idStore.MapConstant("FlightPlan"), "FlightPlan", SimStateUpdate.POLL, SimStateData.STRING) },
                { "Sim", new(idStore.MapConstant("Sim"), "Sim", SimStateUpdate.BOTH, SimStateData.INT) },
                { "1sec", new(idStore.MapConstant("1sec"), "1sec", SimStateUpdate.SUBSCRIBE, SimStateData.INT) },
                { "4sec", new(idStore.MapConstant("4sec"), "4sec", SimStateUpdate.SUBSCRIBE, SimStateData.INT) },
                { "6Hz", new(idStore.MapConstant("6Hz"), "6Hz", SimStateUpdate.SUBSCRIBE, SimStateData.INT) },
                { "Crashed", new(idStore.MapConstant("Crashed"), "Crashed", SimStateUpdate.SUBSCRIBE, SimStateData.INT) },
                { "CrashReset", new(idStore.MapConstant("CrashReset"), "CrashReset", SimStateUpdate.SUBSCRIBE, SimStateData.INT) },
                { "CustomMissionActionExecuted", new(idStore.MapConstant("CustomMissionActionExecuted"), "CustomMissionActionExecuted", SimStateUpdate.SUBSCRIBE, SimStateData.INT) },
                { "FlightSaved", new(idStore.MapConstant("FlightSaved"), "FlightSaved", SimStateUpdate.SUBSCRIBE, SimStateData.STRING) },
                { "FlightPlanActivated", new(idStore.MapConstant("FlightPlanActivated"), "FlightPlanActivated", SimStateUpdate.SUBSCRIBE, SimStateData.STRING) },
                { "FlightPlanDeactivated", new(idStore.MapConstant("FlightPlanDeactivated"), "FlightPlanDeactivated", SimStateUpdate.SUBSCRIBE, SimStateData.INT) },
                { "Frame", new(idStore.MapConstant("Frame"), "Frame", SimStateUpdate.SUBSCRIBE, SimStateData.INT) },
                { "ObjectAdded", new(idStore.MapConstant("ObjectAdded"), "ObjectAdded", SimStateUpdate.SUBSCRIBE, SimStateData.OBJECT) },
                { "ObjectRemoved", new(idStore.MapConstant("ObjectRemoved"), "ObjectRemoved", SimStateUpdate.SUBSCRIBE, SimStateData.OBJECT) },
                { "Pause", new(idStore.MapConstant("Pause"), "Pause", SimStateUpdate.SUBSCRIBE, SimStateData.INT) },
                { "Pause_EX1", new(idStore.MapConstant("Pause_EX1"), "Pause_EX1", SimStateUpdate.SUBSCRIBE, SimStateData.INT) },
                { "Paused", new(idStore.MapConstant("Paused"), "Paused", SimStateUpdate.SUBSCRIBE, SimStateData.INT) },
                { "PauseFrame", new(idStore.MapConstant("PauseFrame"), "PauseFrame", SimStateUpdate.SUBSCRIBE, SimStateData.INT) },
                { "PositionChanged", new(idStore.MapConstant("PositionChanged"), "PositionChanged", SimStateUpdate.SUBSCRIBE, SimStateData.INT) },
                { "SimStart", new(idStore.MapConstant("SimStart"), "SimStart", SimStateUpdate.SUBSCRIBE, SimStateData.INT) },
                { "SimStop", new(idStore.MapConstant("SimStop"), "SimStop", SimStateUpdate.SUBSCRIBE, SimStateData.INT) },
                { "Sound", new(idStore.MapConstant("Sound"), "Sound", SimStateUpdate.SUBSCRIBE, SimStateData.INT) },
                { "Unpaused", new(idStore.MapConstant("Unpaused"), "Unpaused", SimStateUpdate.SUBSCRIBE, SimStateData.INT) },
                { "View", new(idStore.MapConstant("View"), "View", SimStateUpdate.SUBSCRIBE, SimStateData.INT) },
                { "WeatherModeChanged", new(idStore.MapConstant("WeatherModeChanged"), "WeatherModeChanged", SimStateUpdate.SUBSCRIBE, SimStateData.INT) }
            };

            return dict;
        }

        public const string AircraftLoaded = "AircraftLoaded";
        public const string DialogMode = "DialogMode";
        public const string FlightLoaded = "FlightLoaded";
        public const string FlightPlan = "FlightPlan";
        public const string Sim = "Sim";

        public const string OneSec = "1sec";
        public const string FourSec = "4sec";
        public const string SixHz = "6Hz";

        public const string Crashed = "Crashed";
        public const string CrashReset = "CrashReset";
        public const string CustomMissionActionExecuted = "CustomMissionActionExecuted";
        public const string FlightSaved = "FlightSaved";
        public const string FlightPlanActivated = "FlightPlanActivated";
        public const string FlightPlanDeactivated = "FlightPlanDeactivated";
        public const string Frame = "Frame";
        public const string ObjectAdded = "ObjectAdded";
        public const string ObjectRemoved = "ObjectRemoved";
        public const string Pause = "Pause";
        public const string Pause_EX1 = "Pause_EX1";
        public const string Paused = "Paused";
        public const string PauseFrame = "PauseFrame";
        public const string PositionChanged = "PositionChanged";
        public const string SimStart = "SimStart";
        public const string SimStop = "SimStop";
        public const string Sound = "Sound";
        public const string Unpaused = "Unpaused";
        public const string View = "View";
        public const string WeatherModeChanged = "WeatherModeChanged";
    }
}
