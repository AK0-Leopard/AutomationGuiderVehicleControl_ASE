using com.mirle.ibg3k0.bcf.App;
using com.mirle.ibg3k0.bcf.Controller;
using com.mirle.ibg3k0.sc.App;
using com.mirle.ibg3k0.sc.Common;
using com.mirle.ibg3k0.sc.Data.SECS;
using com.mirle.ibg3k0.sc.ProtocolFormat.OHTMessage;
using com.mirle.ibg3k0.stc.Common;
using com.mirle.ibg3k0.stc.Data.SecsData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;

namespace com.mirle.ibg3k0.sc.Data.SECSDriver
{
    public abstract class IBSEMDriver : SEMDriver
    {
        NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();


        #region Receive
        protected abstract void S2F49ReceiveEnhancedRemoteCommandExtension(object sender, SECSEventArgs e);
        protected abstract void S2F41ReceiveHostCommand(object sender, SECSEventArgs e);
        #endregion Receive

        #region Send
        #region Transfer Event

        public abstract bool S6F11SendTransferAbortCompleted(string cmdID, List<AMCSREPORTQUEUE> reportQueues = null);
        public abstract bool S6F11SendTransferAbortFailed(string cmdID, List<AMCSREPORTQUEUE> reportQueues = null);
        public abstract bool S6F11SendTransferAbortInitial(string cmdID, List<AMCSREPORTQUEUE> reportQueues = null);
        public abstract bool S6F11SendTransferCancelCompleted(string cmdID, List<AMCSREPORTQUEUE> reportQueues = null);
        public abstract bool S6F11SendTransferCancelFailed(string cmdID, List<AMCSREPORTQUEUE> reportQueues = null);
        public abstract bool S6F11SendTransferCancelInitial(string cmdID, List<AMCSREPORTQUEUE> reportQueues = null);
        public abstract bool S6F11SendTransferPaused(string cmdID, List<AMCSREPORTQUEUE> reportQueues = null);
        public abstract bool S6F11SendTransferResumed(string cmdID, List<AMCSREPORTQUEUE> reportQueues = null);

        public abstract bool S6F11SendTransferInitial(string cmdID, List<AMCSREPORTQUEUE> reportQueues = null);
        public abstract bool S6F11SendTransferring(string cmdID, List<AMCSREPORTQUEUE> reportQueues = null);
        public abstract bool S6F11SendVehicleArrived(string cmdID, List<AMCSREPORTQUEUE> reportQueues = null);
        public abstract bool S6F11SendVehicleAcquireStarted(string cmdID, List<AMCSREPORTQUEUE> reportQueues = null);
        public abstract bool S6F11SendVehicleAcquireCompleted(string cmdID, List<AMCSREPORTQUEUE> reportQueues = null);
        public abstract bool S6F11SendVehicleAssigned(string cmdID, List<AMCSREPORTQUEUE> reportQueues = null);
        public abstract bool S6F11SendVehicleDeparted(string cmdID, List<AMCSREPORTQUEUE> reportQueues = null);
        public abstract bool S6F11SendVehicleDepositStarted(string cmdID, List<AMCSREPORTQUEUE> reportQueues = null);
        public abstract bool S6F11SendVehicleDepositCompleted(string cmdID, List<AMCSREPORTQUEUE> reportQueues = null);
        public abstract bool S6F11SendCarrierInstalled(string vhID, string carrierID, string location, List<AMCSREPORTQUEUE> reportQueues = null);
        public abstract bool S6F11SendCarrierInstalled(string cmdID, List<AMCSREPORTQUEUE> reportQueues = null);
        public abstract bool S6F11SendCarrierRemoved(string vhID, string carrierID, string location, List<AMCSREPORTQUEUE> reportQueues = null);
        public abstract bool S6F11SendCarrierRemoved(string cmdID, List<AMCSREPORTQUEUE> reportQueues = null);
        public abstract bool S6F11SendVehicleUnassinged(string cmdID, List<AMCSREPORTQUEUE> reportQueues = null);
        public abstract bool S6F11SendTransferCompleted(string cmdID, List<AMCSREPORTQUEUE> reportQueues = null);
        public abstract bool S6F11SendTransferCompleted(VTRANSFER vtransfer, CompleteStatus completeStatus, List<AMCSREPORTQUEUE> reportQueues = null);

        public abstract bool S6F11SendRunTimeStatus(string vhID, List<AMCSREPORTQUEUE> reportQueues = null);
        public abstract bool S6F11SendVehicleInstalled(string cmdID, List<AMCSREPORTQUEUE> reportQueues = null);
        public abstract bool S6F11SendVehicleRemoved(string cmdID, List<AMCSREPORTQUEUE> reportQueues = null);
        #endregion Transfer Event
        #region Port Event
        public abstract bool S6F11PortEventStateChanged(string cmdID, List<AMCSREPORTQUEUE> reportQueues = null);
        #endregion Port Event

        public abstract bool S6F11SendUnitAlarmCleared(string vhID, string transferID, string alarmID, string alarmTest, List<AMCSREPORTQUEUE> reportQueues = null);
        public abstract bool S6F11SendUnitAlarmSet(string vhID, string transferID, string alarmID, string alarmTest, List<AMCSREPORTQUEUE> reportQueues = null);


        #region TSC State Transition Event
        public abstract bool S6F11SendTSCAutoCompleted();
        public abstract bool S6F11SendTSCAutoInitiated();
        public abstract bool S6F11SendTSCPauseCompleted();
        public abstract bool S6F11SendTSCPaused();
        public abstract bool S6F11SendTSCPauseInitiated();
        #endregion TSC State Transition Event


        #endregion Send

    }
}