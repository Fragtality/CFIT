using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace CFIT.SimConnectLib.Definitions
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
    public struct StructString
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string str;

        public override readonly string ToString()
        {
            return str;
        }

        public override readonly bool Equals([NotNullWhen(true)] object? obj)
        {
            if (obj == null)
                return false;
            else if (obj is string value)
                return str.Equals(value);
            else
                return false;
        }

        public static bool operator ==(StructString left, StructString right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(StructString left, StructString right)
        {
            return !left.Equals(right);
        }

        public override readonly int GetHashCode()
        {
            return str.GetHashCode();
        }
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
    public struct StructXYZ
    {
        public double x;
        public double y;
        public double z;

        public override readonly string ToString()
        {
            return $"[x: {x:F9} y: {y:F6} z: {z:F9}]";
        }

        public override readonly bool Equals([NotNullWhen(true)] object? obj)
        {
            if (obj == null)
                return false;
            else if (obj is StructXYZ strct)
                return x == strct.x && y == strct.y && z == strct.z;
            else
                return false;
        }

        public static bool operator ==(StructXYZ left, StructXYZ right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(StructXYZ left, StructXYZ right)
        {
            return !left.Equals(right);
        }

        public override readonly int GetHashCode()
        {
            return x.GetHashCode() ^ y.GetHashCode() ^ z.GetHashCode();
        }
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
    public struct StructPBH
    {
        public float pitch;
        public float bank;
        public float heading;

        public override readonly string ToString()
        {
            return $"[pitch: {pitch:F7} bank: {bank:F7} heading: {heading:F7}]";
        }

        public override readonly bool Equals([NotNullWhen(true)] object? obj)
        {
            if (obj == null)
                return false;
            else if (obj is StructPBH strct)
                return pitch == strct.pitch && bank == strct.bank && heading == strct.heading;
            else
                return false;
        }

        public static bool operator ==(StructPBH left, StructPBH right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(StructPBH left, StructPBH right)
        {
            return !left.Equals(right);
        }

        public override readonly int GetHashCode()
        {
            return pitch.GetHashCode() ^ bank.GetHashCode() ^ heading.GetHashCode();
        }
    };

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
    public struct StructLatLonAltPBH
    {
        public double lat;
        public double lon;
        public double alt;
        public float pitch;
        public float bank;
        public float heading;

        public override readonly string ToString()
        {
            return $"[lat: {lat:F9} lon: {lon:F9} alt: {alt:F9} pitch: {pitch:F7} bank: {bank:F7} heading: {heading:F7}]";
        }

        public override readonly bool Equals([NotNullWhen(true)] object? obj)
        {
            if (obj == null)
                return false;
            else if (obj is StructLatLonAltPBH strct)
                return lat == strct.lat && lon == strct.lon && alt == strct.alt && pitch == strct.pitch && bank == strct.bank && heading == strct.heading;
            else
                return false;
        }

        public static bool operator ==(StructLatLonAltPBH left, StructLatLonAltPBH right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(StructLatLonAltPBH left, StructLatLonAltPBH right)
        {
            return !left.Equals(right);
        }

        public override readonly int GetHashCode()
        {
            return lat.GetHashCode() ^ lon.GetHashCode() ^ alt.GetHashCode() ^ pitch.GetHashCode() ^ bank.GetHashCode() ^ heading.GetHashCode();
        }
    };

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
    public struct StructLatLonAlt
    {
        public double lat;
        public double lon;
        public double alt;

        public override readonly string ToString()
        {
            return $"[lat: {lat:F9} lon: {lon:F9} alt: {alt:F9}]";
        }

        public override readonly bool Equals([NotNullWhen(true)] object? obj)
        {
            if (obj == null)
                return false;
            else if (obj is StructLatLonAlt strct)
                return lat == strct.lat && lon == strct.lon && alt == strct.alt;
            else
                return false;
        }

        public static bool operator ==(StructLatLonAlt left, StructLatLonAlt right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(StructLatLonAlt left, StructLatLonAlt right)
        {
            return !left.Equals(right);
        }

        public override readonly int GetHashCode()
        {
            return lat.GetHashCode() ^ lon.GetHashCode() ^ alt.GetHashCode();
        }
    };

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
    public struct StructPID
    {
        public double pid_p;
        public double pid_i;
        public double pid_i2;
        public double pid_d;
        public double i_boundary;
        public double i2_boundary;
        public double d_boundary;

        public override readonly string ToString()
        {
            return $"[pid_p {pid_d:F9} pid_i {pid_i:F9} pid_i2 {pid_i2:F9} pid_d {pid_d:F9} i_boundary {i_boundary:F9} i2_boundary {i2_boundary:F9} d_boundary {d_boundary:F9}]";
        }

        public override readonly bool Equals([NotNullWhen(true)] object? obj)
        {
            if (obj == null)
                return false;
            else if (obj is StructPID strct)
                return pid_p == strct.pid_p && pid_i == strct.pid_i && pid_i2 == strct.pid_i2 && pid_d == strct.pid_d && i_boundary == strct.i_boundary && i2_boundary == strct.i2_boundary && d_boundary == strct.d_boundary;
            else
                return false;
        }

        public static bool operator ==(StructPID left, StructPID right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(StructPID left, StructPID right)
        {
            return !left.Equals(right);
        }

        public override readonly int GetHashCode()
        {
            return pid_p.GetHashCode() ^ pid_i.GetHashCode() ^ pid_i2.GetHashCode() ^ pid_d.GetHashCode() ^ i_boundary.GetHashCode() ^ i2_boundary.GetHashCode() ^ d_boundary.GetHashCode();
        }
    };

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
    public struct StructFuelLevels
    {
        public float Center;
        public float LeftMain;
        public float RightMain;
        public float LeftAux;
        public float RightAux;
        public float LeftTip;
        public float RightTip;
        public float Center2;
        public float Center3;
        public float External1;
        public float External2;

        public override readonly string ToString()
        {
            return $"[Center {Center:F7} LeftMain {LeftMain:F7} RightMain {RightMain:F7} LeftAux {LeftAux:F7} RightAux {RightAux:F7} LeftTip {LeftTip:F7} RightTip {RightTip:F7} Center2 {Center2:F7} Center3 {Center3:F7} External1 {External1:F7} External2 {External2:F7}]";
        }

        public override readonly bool Equals([NotNullWhen(true)] object? obj)
        {
            if (obj == null)
                return false;
            else if (obj is StructFuelLevels strct)
                return Center == strct.Center && LeftMain == strct.LeftMain && RightMain == strct.RightMain && LeftAux == strct.LeftAux && RightAux == strct.RightAux && LeftTip == strct.LeftTip && RightTip == strct.RightTip && Center2 == strct.Center2 && Center3 == strct.Center3 && External1 == strct.External1 && External2 == strct.External2;
            else
                return false;
        }

        public static bool operator ==(StructFuelLevels left, StructFuelLevels right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(StructFuelLevels left, StructFuelLevels right)
        {
            return !left.Equals(right);
        }

        public override readonly int GetHashCode()
        {
            return Center.GetHashCode() ^ LeftMain.GetHashCode() ^ RightMain.GetHashCode() ^ LeftAux.GetHashCode() ^ RightAux.GetHashCode() ^ LeftTip.GetHashCode() ^ RightTip.GetHashCode() ^ Center2.GetHashCode() ^ Center3.GetHashCode() ^ External1.GetHashCode() ^ External2.GetHashCode();
        }
    };
}
