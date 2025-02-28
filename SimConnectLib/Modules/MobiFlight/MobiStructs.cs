using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Text;

namespace CFIT.SimConnectLib.Modules.MobiFlight
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct MobiVarValue
    {
        public float data;

        public static implicit operator float(MobiVarValue msg)
        {
            return msg.data;
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct MobiStringValue
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public String data;

        public static implicit operator string(MobiStringValue msg)
        {
            return msg.data;
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct MobiMessageBuffer
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = (int)MobiModule.MOBIFLIGHT_MESSAGE_SIZE)]
        public byte[] data;

        public MobiMessageBuffer(string strData)
        {
            byte[] txtBytes = Encoding.ASCII.GetBytes(strData);
            var ret = new byte[1024];
            Array.Copy(txtBytes, ret, txtBytes.Length);
            data = ret;
        }
    }

    public struct MobiMessage
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = (int)MobiModule.MOBIFLIGHT_MESSAGE_SIZE)]
        public String Data;

        public static implicit operator string(MobiMessage msg)
        {
            return msg.ToString();
        }

        public override readonly string ToString()
        {
            return Data ?? "";
        }

        public override readonly bool Equals([NotNullWhen(true)] object? obj)
        {
            if (obj == null)
                return false;
            else if (obj is string value)
                return Data?.Equals(value) == true;
            else
                return false;
        }

        public static bool operator ==(MobiMessage left, MobiMessage right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(MobiMessage left, MobiMessage right)
        {
            return !left.Equals(right);
        }

        public static bool operator ==(MobiMessage left, string right)
        {
            return left.Data?.Equals(right) == true;
        }

        public static bool operator !=(MobiMessage left, string right)
        {
            return left.Data?.Equals(right) == false;
        }

        public override readonly int GetHashCode()
        {
            return Data?.GetHashCode() ?? 0;
        }

    }
}
