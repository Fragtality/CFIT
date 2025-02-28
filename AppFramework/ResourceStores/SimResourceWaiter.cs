using CFIT.AppFramework.AppConfig;
using CFIT.SimConnectLib.SimResources;
using System.Threading;
using System.Threading.Tasks;

namespace CFIT.AppFramework.ResourceStores
{
    public class SimResourceWaiter
    {   
        public virtual ISimResourceSubscription Subscription { get; }
        protected virtual CancellationToken Token { get; }
        protected virtual int CheckIntervalMs { get; }
        protected virtual double ReceivedValue { get; set; } = 0;
        public virtual bool IsReceived { get; protected set; } = false;
        public virtual bool IsCanceled { get; protected set; } = false;

        public SimResourceWaiter(ISimResourceSubscription subscription, CancellationToken token, int checkIntervalMs = ProductDefinitionBase.MessageCheckIntervalMs)
        {
            Subscription = subscription;
            Token = token;
            CheckIntervalMs = checkIntervalMs;
            subscription.OnReceived += Receive;
        }

        protected virtual void Receive(ISimResourceSubscription subscription, object value)
        {
            IsReceived = true;
            ReceivedValue = subscription.GetNumber();
        }

        public async Task<double> WaitValueAsync(double value)
        {
            while (!IsReceived && ReceivedValue != value && !IsCanceled && !Token.IsCancellationRequested)
            {
                await Task.Delay(CheckIntervalMs, Token);
            }

            return ReceivedValue;
        }

        public virtual void Cancel()
        {
            IsReceived = false;
            IsCanceled = true;
        }

        public virtual void Dispose()
        {
            IsReceived = false;
            IsCanceled = true;
            Subscription.OnReceived -= Receive;
        }
    }
}
