using CFIT.AppLogger;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CFIT.SimConnectLib.Definitions
{
    public class MappedIdStore(uint startId, uint size)
    {
        protected virtual uint CurrentId { get; set; } = startId;
        public virtual uint MinId { get; } = startId;
        public virtual uint MaxId { get; } = startId + size - 1;
        public virtual uint Size { get { return (MaxId - MinId) + 1; } }
        protected virtual List<MappedID> MappedIds { get; } = [];
        public virtual uint Count { get { return (uint)MappedIds.Count; } }
        public virtual uint DynamicCount { get { return (uint)(MappedIds.Count - ConstantIds.Count); } }
        protected virtual Dictionary<string, MappedID> ConstantIds { get; } = [];
        public virtual List<MappedID> Constants { get { return [.. ConstantIds.Values]; } }

        public virtual MappedID GetNext()
        {
            if (CurrentId + 1 > MaxId)
            {
                Logger.Error($"All IDs allocated - CurrentId {CurrentId} MinId {MinId} MaxId {MaxId} Size {Size} MappedIds {MappedIds.Count} ConstantIds {ConstantIds.Count}");
                throw new Exception("All IDs in MappedIdStore allocated!");
            }

            var id = new MappedID(CurrentId++);
            MappedIds.Add(id);
            return id;
        }

        public MappedID PeekNext()
        {
            return new MappedID(CurrentId);
        }

        public uint GetOffset()
        {
            return DynamicCount - 1;
        }

        public virtual void Reset(bool constants = false)
        {
            MappedIds.Clear();
            if (constants || ConstantIds.Count == 0)
            {
                ConstantIds.Clear();
                CurrentId = MinId;
            }
            else
            {
                MappedIds.AddRange(ConstantIds.Values);
                CurrentId = ConstantIds.Values.Max(x => x.NumId) + 1;
            }
        }

        public virtual void SetHighestMax()
        {
            CurrentId = MappedIds.Max(x => x.NumId) + 1;
        }

        public void SetCurrent(uint value)
        {
            CurrentId = value;
        }

        public virtual bool Contains(int? id)
        {
            return Contains((uint?)id);
        }

        public virtual bool Contains(uint? id)
        {
            if (id == null)
                return false;
            else
                return MappedIds.Where(x => (x.NumId == id)).Any();
        }

        public virtual bool Contains(Enum? id)
        {
            if (id == null)
                return false;
            else
                return MappedIds.Where(x => (x.EnumId.Equals(id))).Any();
        }
        public virtual MappedID MapConstant(string name)
        {
            if (ConstantIds.ContainsKey(name))
            {
                Logger.Warning($"Id already mapped for '{name}'");
                return MappedID.Default();
            }

            var id = GetNext();
            ConstantIds.Add(name, id);
            Logger.Verbose($"Mapped '{name}' to {id}");
            return id;
        }

        public virtual MappedID GetId(string name)
        {
            if (ConstantIds.TryGetValue(name, out var id))
                return id;
            else
            {
                Logger.Warning($"No Id mapped for '{name}'");
                return MappedID.Default();
            }
        }

        public virtual bool HasId(string name)
        {
            return ConstantIds.ContainsKey(name);
        }

        public virtual bool HasId(string name, out MappedID id)
        {
            return ConstantIds.TryGetValue(name, out id);
        }
    }
}
