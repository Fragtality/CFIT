using CFIT.SimConnectLib.Definitions;
using Microsoft.FlightSimulator.SimConnect;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace CFIT.SimConnectLib.SimVars
{
    public enum SimCastType
    {
        UNKNOWN = 0,
        BOOL,
        INT,
        LONG,
        FLOAT,
        DOUBLE,
        STRING,
        STRUCT_XYZ,
        STRUCT_PBH,
        STRUCT_LLAPBH,
        STRUCT_LLA,
        STRUCT_PID,
        STRUCT_FUEL
    }

    public class SimUnitType
    {
        public virtual string TypeName { get; protected set; }
        public virtual SimCastType CastType { get; protected set; }
        public virtual SIMCONNECT_DATATYPE DataType { get; protected set; }

        public SimUnitType(string typeName, SimCastType castType = SimCastType.UNKNOWN, SIMCONNECT_DATATYPE dataType = SIMCONNECT_DATATYPE.INVALID)
        {
            TypeName = typeName.ToLowerInvariant();
            CastType = (castType != SimCastType.UNKNOWN ? castType : GetCastType(typeName));
            DataType = (dataType != SIMCONNECT_DATATYPE.INVALID ? dataType : GetDataType(typeName));
        }

        public static SimCastType GetCastType(string typeName)
        {
            return typeName switch
            {
                String => SimCastType.STRING,
                Bool => SimCastType.BOOL,
                Enum or Flags or Mask => SimCastType.LONG,
                StructXYZ => SimCastType.STRUCT_XYZ,
                StructPBH => SimCastType.STRUCT_PBH,
                StructLLA => SimCastType.STRUCT_LLA,
                StructLLAPBH => SimCastType.STRUCT_LLAPBH,
                StructPID => SimCastType.STRUCT_PID,
                StructFuel => SimCastType.STRUCT_FUEL,
                _ => SimCastType.DOUBLE,
            };
        }

        public static SIMCONNECT_DATATYPE GetDataType(string typeName)
        {
            var dataType = typeName switch
            {
                String => SIMCONNECT_DATATYPE.STRING256,
                Bool => SIMCONNECT_DATATYPE.INT32,
                Enum or Flags or Mask => SIMCONNECT_DATATYPE.INT64,
                StructXYZ or StructPBH or StructLLA or StructLLAPBH or StructPID or StructFuel => SIMCONNECT_DATATYPE.XYZ,
                _ => SIMCONNECT_DATATYPE.FLOAT64,
            };
            return dataType;
        }

        public virtual string GetDefinitionName()
        {
            return TypeName switch
            {
                String or StructXYZ or StructPBH or StructLLA or StructLLAPBH or StructPID or StructFuel => null,
                _ => TypeName,
            };
        }

        public virtual void RegisterDefineStruct(MappedID id, SimConnectManager manager)
        {
            switch (CastType)
            {
                case SimCastType.DOUBLE:
                    manager.Call(sc => sc.RegisterDataDefineStruct<double>(id));
                    break;
                case SimCastType.FLOAT:
                    manager.Call(sc => sc.RegisterDataDefineStruct<float>(id));
                    break;
                case SimCastType.INT:
                    manager.Call(sc => sc.RegisterDataDefineStruct<int>(id));
                    break;
                case SimCastType.LONG:
                    manager.Call(sc => sc.RegisterDataDefineStruct<long>(id));
                    break;
                case SimCastType.BOOL:
                    manager.Call(sc => sc.RegisterDataDefineStruct<bool>(id));
                    break;
                case SimCastType.STRING:
                    manager.Call(sc => sc.RegisterDataDefineStruct<StructString>(id));
                    break;
                case SimCastType.STRUCT_XYZ:
                    manager.Call(sc => sc.RegisterDataDefineStruct<StructXYZ>(id));
                    break;
                case SimCastType.STRUCT_PBH:
                    manager.Call(sc => sc.RegisterDataDefineStruct<StructPBH>(id));
                    break;
                case SimCastType.STRUCT_LLA:
                    manager.Call(sc => sc.RegisterDataDefineStruct<StructLatLonAlt>(id));
                    break;
                case SimCastType.STRUCT_LLAPBH:
                    manager.Call(sc => sc.RegisterDataDefineStruct<StructLatLonAltPBH>(id));
                    break;
                case SimCastType.STRUCT_PID:
                    manager.Call(sc => sc.RegisterDataDefineStruct<StructPID>(id));
                    break;
                case SimCastType.STRUCT_FUEL:
                    manager.Call(sc => sc.RegisterDataDefineStruct<StructFuelLevels>(id));
                    break;
                default:
                    manager.Call(sc => sc.RegisterDataDefineStruct<double>(id));
                    break;
            }
        }

        public override string ToString()
        {
            return TypeName;
        }

        public static implicit operator string(SimUnitType type)
        {
            return type.ToString();
        }

        public override bool Equals(object? obj)
        {
            if (obj is SimUnitType t)
                return this.TypeName == t.TypeName;
            else if (obj is string s)
                return this.TypeName == s;
            else
                return false;
        }

        public static bool operator ==(SimUnitType left, SimUnitType right)
        {
            return left?.TypeName == right?.TypeName;
        }

        public static bool operator !=(SimUnitType left, SimUnitType right)
        {
            return left?.TypeName != right?.TypeName;
        }

        public static bool operator ==(SimUnitType left, string right)
        {
            return left?.TypeName == right;
        }

        public static bool operator !=(SimUnitType left, string right)
        {
            return left?.TypeName != right;
        }

        public override int GetHashCode()
        {
            return TypeName.GetHashCode();
        }

        public bool IsNumeric()
        {
            return IsDataTypeNumeric(DataType);
        }

        public static bool IsDataTypeNumeric(SIMCONNECT_DATATYPE type)
        {
            return type == SIMCONNECT_DATATYPE.INT64 || type == SIMCONNECT_DATATYPE.INT32 || type == SIMCONNECT_DATATYPE.FLOAT32 || type == SIMCONNECT_DATATYPE.FLOAT64;
        }

        public bool IsString()
        {
            return IsDataTypeString(DataType);
        }

        public static bool IsDataTypeString(SIMCONNECT_DATATYPE type)
        {
            return type == SIMCONNECT_DATATYPE.STRING256 || type == SIMCONNECT_DATATYPE.STRINGV || type == SIMCONNECT_DATATYPE.STRING128;
        }

        public bool IsStruct()
        {
            return IsCastTypeStruct(CastType);
        }

        public static bool IsCastTypeStruct(SimCastType type)
        {
            return type >= SimCastType.STRUCT_XYZ;
        }

        public static List<string> GetTypeList()
        {
            var query = typeof(SimUnitType).GetRuntimeFields().Where(i => i.IsLiteral);
            var obj = new SimUnitType("number");
            return query.Select(f => f.GetRawConstantValue().ToString()).ToList();
        }

        public const string Ac = "ac";
        public const string Acre = "acre";
        public const string Acres = "acres";
        public const string Amp = "amp";
        public const string Ampere = "ampere";
        public const string Amperes = "amperes";
        public const string Amps = "amps";
        public const string Angl16 = "angl16";
        public const string Angl32 = "angl32";
        public const string Atm = "atm";
        public const string Atmosphere = "atmosphere";
        public const string Atmospheres = "atmospheres";
        public const string Bar = "bar";
        public const string Bars = "bars";
        public const string Bco16 = "Bco16";
        public const string Bel = "bel";
        public const string Bels = "bels";
        public const string Bool = "Bool";
        public const string BoostCmHg = "boost cmHg";
        public const string BoostInHg = "boost inHg";
        public const string BoostPsi = "boost psi";
        public const string Celsius = "celsius";
        public const string CelsiusFs7Egt = "celsius fs7 egt";
        public const string CelsiusFs7OilTemp = "celsius fs7 oil temp";
        public const string CelsiusScaler1_256 = "celsius scaler 1/256";
        public const string CelsiusScaler16k = "celsius scaler 16k";
        public const string CelsiusScaler256 = "celsius scaler 256";
        public const string Centimeter = "centimeter";
        public const string CentimeterOfMercury = "centimeter of mercury";
        public const string Centimeters = "centimeters";
        public const string CentimetersOfMercury = "centimeters of mercury";
        public const string Cm = "cm";
        public const string Cm2 = "cm2";
        public const string Cm3 = "cm3";
        public const string CmHg = "cmHg";
        public const string CubicCentimeter = "cubic centimeter";
        public const string CubicCentimeters = "cubic centimeters";
        public const string CubicFeet = "cubic feet";
        public const string CubicFoot = "cubic foot";
        public const string CubicInch = "cubic inch";
        public const string CubicInches = "cubic inches";
        public const string CubicKilometer = "cubic kilometer";
        public const string CubicKilometers = "cubic kilometers";
        public const string CubicMeter = "cubic meter";
        public const string CubicMeters = "cubic meters";
        public const string CubicMile = "cubic mile";
        public const string CubicMiles = "cubic miles";
        public const string CubicMillimeter = "cubic millimeter";
        public const string CubicMillimeters = "cubic millimeters";
        public const string CubicYard = "cubic yard";
        public const string CubicYards = "cubic yards";
        public const string CuCm = "cu cm";
        public const string CuFt = "cu ft";
        public const string CuIn = "cu in";
        public const string CuKm = "cu km";
        public const string CuM = "cu m";
        public const string CuMm = "cu mm";
        public const string CuYd = "cu yd";
        public const string Day = "day";
        public const string Days = "days";
        public const string Decibel = "decibel";
        public const string Decibels = "decibels";
        public const string Decimile = "decimile";
        public const string Decimiles = "decimiles";
        public const string Decinmile = "decinmile";
        public const string Decinmiles = "decinmiles";
        public const string Degree = "degree";
        public const string DegreeAngl16 = "degree angl16";
        public const string DegreeAngl32 = "degree angl32";
        public const string DegreeLatitude = "degree latitude";
        public const string DegreeLongitude = "degree longitude";
        public const string DegreePerSecond = "degree per second";
        public const string DegreePerSecondAng16 = "degree per second ang16";
        public const string DegreePerSecondSquared = "degree per second squared";
        public const string Degrees = "degrees";
        public const string DegreesAngl16 = "degrees angl16";
        public const string DegreesAngl32 = "degrees angl32";
        public const string DegreesLatitude = "degrees latitude";
        public const string DegreesLongitude = "degrees longitude";
        public const string DegreesPerSecond = "degrees per second";
        public const string DegreesPerSecondAng16 = "degrees per second ang16";
        public const string DegreesPerSecondSquared = "degrees per second squared";
        public const string Enum = "Enum";
        public const string Fahrenheit = "fahrenheit";
        public const string Farenheit = "farenheit";
        public const string Feet = "feet";
        public const string FeetPerMinute = "feet per minute";
        public const string FeetPerSecond = "feet per second";
        public const string FeetPerSecondSquared = "feet per second squared";
        public const string Flags = "flags";
        public const string Foot = "foot";
        public const string FootPerSecondSquared = "foot per second squared";
        public const string FootPound = "foot pound";
        public const string FootPounds = "foot-pounds";
        public const string FrequencyADFBCD32 = "Frequency ADF BCD32";
        public const string FrequencyBCD16 = "Frequency BCD16";
        public const string FrequencyBCD32 = "Frequency BCD32";
        public const string Fs7ChargingAmps = "fs7 charging amps";
        public const string Fs7OilQuantity = "fs7 oil quantity";
        public const string Ft = "ft";
        public const string Ft2 = "ft2";
        public const string Ft3 = "ft3";
        public const string FtLbPerSecond = "ft lb per second";
        public const string FtLbs = "ft-lbs";
        public const string Gallon = "gallon";
        public const string GallonPerHour = "gallon per hour";
        public const string Gallons = "gallons";
        public const string GallonsPerHour = "gallons per hour";
        public const string Geepound = "geepound";
        public const string Geepounds = "geepounds";
        public const string GForce = "GForce";
        public const string GForce624Scaled = "G Force 624 scaled";
        public const string Gph = "gph";
        public const string Grad = "grad";
        public const string Grads = "grads";
        public const string Ha = "ha";
        public const string Half = "half";
        public const string Halfs = "halfs";
        public const string Hectare = "hectare";
        public const string Hectares = "hectares";
        public const string Hectopascal = "hectopascal";
        public const string Hectopascals = "hectopascals";
        public const string Hertz = "Hertz";
        public const string Hour = "hour";
        public const string HourOver10 = "hour over 10";
        public const string Hours = "hours";
        public const string HoursOver10 = "hours over 10";
        public const string Hz = "Hz";
        public const string In = "in";
        public const string In2 = "in2";
        public const string In3 = "in3";
        public const string Inch = "inch";
        public const string Inches = "inches";
        public const string InchesOfMercury = "inches of mercury";
        public const string InchOfMercury = "inch of mercury";
        public const string InHg = "inHg";
        public const string InHg64Over64k = "inHg 64 over 64k";
        public const string Kelvin = "kelvin";
        public const string Keyframe = "keyframe";
        public const string Keyframes = "keyframes";
        public const string Kg = "kg";
        public const string KgfMeter = "kgf meter";
        public const string KgfMeters = "kgf meters";
        public const string KgFSqCm = "KgFSqCm";
        public const string KHz = "KHz";
        public const string Kilogram = "kilogram";
        public const string KilogramForcePerSquareCentimeter = "kilogram force per square centimeter";
        public const string KilogramMeter = "kilogram meter";
        public const string KilogramMeters = "kilogram meters";
        public const string KilogramMeterSquared = "kilogram meter squared";
        public const string KilogramPerCubicMeter = "kilogram per cubic meter";
        public const string KilogramPerSecond = "kilogram per second";
        public const string Kilograms = "kilograms";
        public const string KilogramsMeterSquared = "kilograms meter squared";
        public const string KilogramsPerCubicMeter = "kilograms per cubic meter";
        public const string KilogramsPerSecond = "kilograms per second";
        public const string Kilohertz = "Kilohertz";
        public const string Kilometer = "kilometer";
        public const string KilometerPerHour = "kilometer per hour";
        public const string Kilometers = "kilometers";
        public const string KilometersPerHour = "kilometers per hour";
        public const string Kilopascal = "kilopascal";
        public const string Km = "km";
        public const string Km2 = "km2";
        public const string Km3 = "km3";
        public const string Knot = "knot";
        public const string Knots = "knots";
        public const string KnotScaler128 = "knot scaler 128";
        public const string KnotsScaler128 = "knots scaler 128";
        public const string KPa = "kPa";
        public const string Kph = "kph";
        public const string Lbf = "lbf";
        public const string LbfFeet = "lbf-feet";
        public const string Lbs = "lbs";
        public const string Liter = "liter";
        public const string LiterPerHour = "liter per hour";
        public const string Liters = "liters";
        public const string LitersPerHour = "liters per hour";
        public const string M = "m";
        public const string M_s = "m/s";
        public const string M2 = "m2";
        public const string M3 = "m3";
        public const string Mach = "mach";
        public const string Mach3d2Over64k = "mach 3d2 over 64k";
        public const string Machs = "machs";
        public const string Mask = "mask";
        public const string Mbar = "mbar";
        public const string Mbars = "mbars";
        public const string Megahertz = "Megahertz";
        public const string Meter = "meter";
        public const string Meter_second = "meter/second";
        public const string MeterCubed = "meter cubed";
        public const string MeterCubedPerSecond = "meter cubed per second";
        public const string MeterLatitude = "meter latitude";
        public const string MeterPerMinute = "meter per minute";
        public const string MeterPerSecond = "meter per second";
        public const string MeterPerSecondScaler256 = "meter per second scaler 256";
        public const string MeterPerSecondSquared = "meter per second squared";
        public const string Meters = "meters";
        public const string MeterScaler256 = "meter scaler 256";
        public const string MetersCubed = "meters cubed";
        public const string MetersCubedPerSecond = "meters cubed per second";
        public const string MetersLatitude = "meters latitude";
        public const string MetersPerMinute = "meters per minute";
        public const string MetersPerSecond = "meters per second";
        public const string MetersPerSecondScaler256 = "meters per second scaler 256";
        public const string MetersPerSecondSquared = "meters per second squared";
        public const string MetersScaler256 = "meters scaler 256";
        public const string MHz = "MHz";
        public const string Mile = "mile";
        public const string MilePerHour = "mile per hour";
        public const string Miles = "miles";
        public const string MilesPerHour = "miles per hour";
        public const string Millibar = "millibar";
        public const string Millibars = "millibars";
        public const string MillibarScaler16 = "millibar scaler 16";
        public const string MillibarsScaler16 = "millibars scaler 16";
        public const string Millimeter = "millimeter";
        public const string MillimeterOfMercury = "millimeter of mercury";
        public const string MillimeterOfWater = "millimeter of water";
        public const string Millimeters = "millimeters";
        public const string MillimetersOfMercury = "millimeters of mercury";
        public const string MillimetersOfWater = "millimeters of water";
        public const string Minute = "minute";
        public const string MinutePerRound = "minute per round";
        public const string Minutes = "minutes";
        public const string MinutesPerRound = "minutes per round";
        public const string Mm2 = "mm2";
        public const string Mm3 = "mm3";
        public const string MmHg = "mmHg";
        public const string More_than_a_half = "more_than_a_half";
        public const string Mph = "mph";
        public const string N = "N";
        public const string NauticalMile = "nautical mile";
        public const string NauticalMiles = "nautical miles";
        public const string Newton = "newton";
        public const string NewtonMeter = "newton meter";
        public const string NewtonMeters = "newton meters";
        public const string NewtonPerSquareMeter = "newton per square meter";
        public const string Newtons = "newtons";
        public const string NewtonsPerSquareMeter = "newtons per square meter";
        public const string NiceMinutePerRound = "nice minute per round";
        public const string NiceMinutesPerRound = "nice minutes per round";
        public const string Nm = "Nm";
        public const string Nmile = "nmile";
        public const string Nmiles = "nmiles";
        public const string Number = "number";
        public const string Pa = "Pa";
        public const string Part = "part";
        public const string Pascal = "pascal";
        public const string Pascals = "pascals";
        public const string Percent = "percent";
        public const string Percentage = "percentage";
        public const string PercentOver100 = "percent over 100";
        public const string PercentScaler16k = "percent scaler 16k";
        public const string PercentScaler2pow23 = "percent scaler 2pow23";
        public const string PercentScaler32k = "percent scaler 32k";
        public const string PerDegree = "per degree";
        public const string PerHour = "per hour";
        public const string PerMinute = "per minute";
        public const string PerRadian = "per radian";
        public const string PerSecond = "per second";
        public const string Position = "position";
        public const string Position128 = "position 128";
        public const string Position16k = "position 16k";
        public const string Position32k = "position 32k";
        public const string Pound = "pound";
        public const string PoundalFeet = "poundal feet";
        public const string PoundForcePerSquareFoot = "pound-force per square foot";
        public const string PoundForcePerSquareInch = "pound-force per square inch";
        public const string PoundPerHour = "pound per hour";
        public const string Pounds = "pounds";
        public const string PoundScaler256 = "pound scaler 256";
        public const string PoundsPerHour = "pounds per hour";
        public const string PoundsScaler256 = "pounds scaler 256";
        public const string Pph = "pph";
        public const string Psf = "psf";
        public const string PsfScaler16k = "psf scaler 16k";
        public const string Psi = "psi";
        public const string Psi4Over16k = "psi 4 over 16k";
        public const string PsiFs7OilPressure = "psi fs7 oil pressure";
        public const string PsiScaler16k = "psi scaler 16k";
        public const string Quart = "quart";
        public const string Quarts = "quarts";
        public const string Radian = "radian";
        public const string RadianPerSecond = "radian per second";
        public const string RadianPerSecondSquared = "radian per second squared";
        public const string Radians = "radians";
        public const string RadiansPerSecond = "radians per second";
        public const string RadiansPerSecondSquared = "radians per second squared";
        public const string Rankine = "rankine";
        public const string Ratio = "ratio";
        public const string RevolutionPerMinute = "revolution per minute";
        public const string RevolutionsPerMinute = "revolutions per minute";
        public const string Round = "round";
        public const string Rounds = "rounds";
        public const string Rpm = "rpm";
        public const string Rpm1Over16k = "rpm 1 over 16k";
        public const string Rpms = "rpms";
        public const string Scaler = "scaler";
        public const string Second = "second";
        public const string Seconds = "seconds";
        public const string Slug = "slug";
        public const string Slug_ft3 = "Slug/ft3";
        public const string SlugFeetSquared = "slug feet squared";
        public const string SlugPerCubicFeet = "Slug per cubic feet";
        public const string SlugPerCubicFoot = "Slug per cubic foot";
        public const string Slugs = "slugs";
        public const string SlugsFeetSquared = "slugs feet squared";
        public const string SlugsPerCubicFeet = "Slugs per cubic feet";
        public const string SlugsPerCubicFoot = "Slugs per cubic foot";
        public const string SqCm = "sq cm";
        public const string SqFt = "sq ft";
        public const string SqIn = "sq in";
        public const string SqKm = "sq km";
        public const string SqM = "sq m";
        public const string SqMm = "sq mm";
        public const string SquareCentimeter = "square centimeter";
        public const string SquareCentimeters = "square centimeters";
        public const string SquareFeet = "square feet";
        public const string SquareFoot = "square foot";
        public const string SquareInch = "square inch";
        public const string SquareInches = "square inches";
        public const string SquareKilometer = "square kilometer";
        public const string SquareKilometers = "square kilometers";
        public const string SquareMeter = "square meter";
        public const string SquareMeters = "square meters";
        public const string SquareMile = "square mile";
        public const string SquareMiles = "square miles";
        public const string SquareMillimeter = "square millimeter";
        public const string SquareMillimeters = "square millimeters";
        public const string SquareYard = "square yard";
        public const string SquareYards = "square yards";
        public const string SqYd = "sq yd";
        public const string String = "string";
        public const string Third = "third";
        public const string Thirds = "thirds";
        public const string Times = "times";
        public const string Volt = "volt";
        public const string Volts = "volts";
        public const string Watt = "Watt";
        public const string Watts = "Watts";
        public const string Yard = "yard";
        public const string Yards = "yards";
        public const string Yd2 = "yd2";
        public const string Yd3 = "yd3";
        public const string Year = "year";
        public const string Years = "years";
        public const string StructXYZ = "Struct XYZ";
        public const string StructPBH = "Struct PBH";
        public const string StructLLA = "Struct LLA";
        public const string StructLLAPBH = "Struct LLAPBH";
        public const string StructPID = "Struct PID";
        public const string StructFuel = "Struct Fuel";
    }
}
