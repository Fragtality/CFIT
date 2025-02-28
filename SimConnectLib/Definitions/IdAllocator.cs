namespace CFIT.SimConnectLib.Definitions
{
    public enum ID_TYPE
    {
        CLIENT_DATA_ID = 1,
        DEFINE_ID = 2,
        REQUEST_ID = 4,
        EVENT_ID = 8,
        NOTIFY_GROUP_ID = 16,
    }

    public class IdAllocator(uint idBase)
    {
        protected virtual uint ClientDataId { get; set; } = idBase;
        protected virtual uint DefineId { get; set; } = idBase;
        protected virtual uint RequestId { get; set; } = idBase;
        protected virtual uint EventId { get; set; } = idBase;
        protected virtual uint NotifyGroupId { get; set; } = idBase;

        public uint GetValue(ID_TYPE idType)
        {
            return idType switch
            {
                ID_TYPE.CLIENT_DATA_ID => ClientDataId,
                ID_TYPE.DEFINE_ID => DefineId,
                ID_TYPE.REQUEST_ID => RequestId,
                ID_TYPE.EVENT_ID => EventId,
                ID_TYPE.NOTIFY_GROUP_ID => NotifyGroupId,
                _ => DefineId,
            };
        }

        public MappedIdStore AllocateStore(uint size, ID_TYPE mainType)
        {
            return AllocateStore(size, mainType, mainType);
        }

        public MappedIdStore AllocateStore(uint size, ID_TYPE mainType, ID_TYPE increaseTypes)
        {
            var store = new MappedIdStore(GetValue(mainType), size);

            if (increaseTypes.HasFlag(ID_TYPE.CLIENT_DATA_ID))
                ClientDataId += size;
            if (increaseTypes.HasFlag(ID_TYPE.DEFINE_ID) || increaseTypes.HasFlag(ID_TYPE.REQUEST_ID))
            {
                DefineId += size;
                RequestId += size;
            }
            if (increaseTypes.HasFlag(ID_TYPE.EVENT_ID))
                EventId += size;
            if (increaseTypes.HasFlag(ID_TYPE.NOTIFY_GROUP_ID))
                NotifyGroupId += size;

            return store;
        }
    }
}
