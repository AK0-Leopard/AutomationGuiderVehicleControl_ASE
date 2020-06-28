//*********************************************************************************
//      MESDefaultMapAction.cs
//*********************************************************************************
// File Name: MESDefaultMapAction.cs
// Description: Type 1 Function
//
//(c) Copyright 2014, MIRLE Automation Corporation
//
// Date          Author         Request No.    Tag     Description
// ------------- -------------  -------------  ------  -----------------------------
//**********************************************************************************
using com.mirle.ibg3k0.bcf.Controller;
using com.mirle.ibg3k0.bcf.Data.TimerAction;
using com.mirle.ibg3k0.sc.App;
using NLog;
using System;

namespace com.mirle.ibg3k0.sc.Data.TimerAction
{
    public class TransferCommandTimerActionMIx : ITimerAction
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();
        protected SCApplication scApp = null;
        protected MPLCSMControl smControl;


        public TransferCommandTimerActionMIx(string name, long intervalMilliSec)
            : base(name, intervalMilliSec)
        {

        }

        public override void initStart()
        {
            scApp = SCApplication.getInstance();
        }

        public override void doProcess(object obj)
        {
            try
            {
                //scApp.TransferService.Scan();
                switch (DebugParameter.TransferMode)
                {
                    case DebugParameter.TransferModeType.Normal:
                        scApp.TransferService.ScanByVTransfer();
                        break;
                    case DebugParameter.TransferModeType.Double:
                        scApp.TransferService.ScanByVTransfer_v2();
                        break;
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Exception");
            }
        }


    }

}
