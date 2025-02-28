using CFIT.SimConnectLib.SimResources;
using System;
using System.Windows.Threading;

namespace CFIT.SimConnectLib.SimStates
{
    public partial class SimStateSubscription : SimResourceSubscription<SimStateManager, SimState, SimStateSubscription>
    {
        protected virtual bool FirstUpdate { get; set; } = true;
     
        protected virtual DispatcherTimer PollTimer { get; set; }
        public virtual int PollInterval { get; protected set; }
        public virtual bool PollOnly { get; protected set; }

        public SimStateSubscription(SimState state, int pollInterval, bool pollOnly) : base(state)
        {
            PollTimer = new DispatcherTimer();
            PollTimer.Tick += PollTick;
            SetPollInterval(pollInterval);
            SetPollOnly(pollOnly);
        }

        protected override bool ChangeCondition()
        {
            return true;
        }

        public override void Update()
        {
            base.Update();
            FirstUpdate = false;
        }

        protected override bool BlockCallback()
        {
            if (CompareEqual() && Resource.UpdateType == SimStateUpdate.POLL && !FirstUpdate)
                return true;

            if (Resource.PollEvent && PollOnly && CompareEqual() && Resource.UpdateType == SimStateUpdate.BOTH && !FirstUpdate)
                return true;

            if (!Resource.PollEvent && PollOnly)
                return true;

            return false;
        }

        protected virtual void PollTick(object? sender, EventArgs e)
        {
            Resource.Request();
        }

        public virtual void SetPollInterval(int pollInterval)
        {
            PollInterval = pollInterval;
            if (PollInterval != -1)
                PollTimer.Interval = TimeSpan.FromMilliseconds(PollInterval);
        }

        public virtual void SetPollOnly(bool pollOnly)
        {
            PollOnly = pollOnly;
            if (!PollTimer.IsEnabled && PollInterval != -1 && (Resource.UpdateType == SimStateUpdate.BOTH && PollOnly) || Resource.UpdateType == SimStateUpdate.POLL)
                PollTimer.Start();
            if ((PollTimer.IsEnabled && ((Resource.UpdateType == SimStateUpdate.BOTH && !PollOnly) || Resource.UpdateType == SimStateUpdate.SUBSCRIBE)) || PollTimer.IsEnabled && PollInterval == -1)
                PollTimer.Stop();
        }
    }
}
