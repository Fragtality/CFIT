using System;
using System.Diagnostics.CodeAnalysis;

namespace CFIT.SimConnectLib.Definitions
{
    public class MappedID(uint id)
    {
        public static readonly MappedID SYSTEM = new(uint.MaxValue);
        private enum DummyEnum { }

        public virtual uint NumId { get; } = id;
        public virtual Enum EnumId { get { return Convert(NumId); } }

        public static MappedID Default()
        {
            return new MappedID(0);
        }

        public virtual Enum Convert(int id)
        {
            return (DummyEnum)id;
        }

        public virtual Enum Convert(uint id)
        {
            return (DummyEnum)id;
        }
        public virtual uint Convert(Enum id)
        {
            return (uint)(object)id;
        }

        public static implicit operator uint(MappedID id)
        {
            return id.NumId;
        }

        public static implicit operator int(MappedID id)
        {
            return (int)id.NumId;
        }

        public static implicit operator Enum(MappedID id)
        {
            return id.EnumId;
        }

        public override string ToString()
        {
            return $"{NumId}";
        }

        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            if (obj == null)
                return false;
            else if (obj is MappedID value)
                return NumId.Equals(value);
            else
                return false;
        }

        public static bool operator ==(MappedID left, MappedID right)
        {
            return left?.Equals(right) == true;
        }

        public static bool operator !=(MappedID left, MappedID right)
        {
            return left?.Equals(right) == false;
        }

        public override int GetHashCode()
        {
            return NumId.GetHashCode();
        }
    }
}
