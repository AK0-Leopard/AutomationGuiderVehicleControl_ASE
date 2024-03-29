﻿using com.mirle.ibg3k0.bcf.App;
using com.mirle.ibg3k0.bcf.Common;
using com.mirle.ibg3k0.bcf.Data.ValueDefMapAction;
using com.mirle.ibg3k0.bcf.Data.VO;
using com.mirle.ibg3k0.sc.App;
using com.mirle.ibg3k0.sc.Data.SECS;
using com.mirle.ibg3k0.sc.Data.VO;
using com.mirle.ibg3k0.sc.Data.VO.Interface;
using com.mirle.ibg3k0.sc.ObjectRelay;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace com.mirle.ibg3k0.sc
{
    public partial class ANODE : BaseEQObject, IAlarmHisList
    {
        public event EventHandler<bool> RedisConnectionStatusChange;

        private AlarmHisList alarmHisList = new AlarmHisList();
        public bool RedisConnectionValid = true;
        public long syncPortStatusUpdate = 0;

        public override void doShareMemoryInit(BCFAppConstants.RUN_LEVEL runLevel)
        {
            foreach (IValueDefMapAction action in valueDefMapActionDic.Values)
            {
                action.doShareMemoryInit(runLevel);
            }
            //對sub eqpt進行初始化
            List<AEQPT> subEqptList = SCApplication.getInstance().getEQObjCacheManager().getEuipmentListByNode(NODE_ID);
            if (subEqptList != null)
            {
                foreach (AEQPT eqpt in subEqptList)
                {
                    eqpt.doShareMemoryInit(runLevel);
                }
            }
            List<AVEHICLE> subVhList = SCApplication.getInstance().getEQObjCacheManager().getAllVehicle();
            if (subVhList != null)
            {
                foreach (AVEHICLE vh in subVhList)
                {
                    vh.doShareMemoryInit(runLevel);
                }
            }
        }

        private bool isredisconnectionfail;
        public bool IsRedisConnectionFail
        {
            set
            {
                if (isredisconnectionfail != value)
                {
                    isredisconnectionfail = value;
                    onRedisConnectionStatusChange();
                }
            }
        }
        private void onRedisConnectionStatusChange()
        {
            try
            {
                RedisConnectionStatusChange?.Invoke(this, isredisconnectionfail);
            }
            catch (Exception ex)
            {
                NLog.LogManager.GetCurrentClassLogger().Error(ex);
            }
        }

        public virtual void resetAlarmHis(List<ALARM> AlarmHisList)
        {
            alarmHisList.resetAlarmHis(AlarmHisList);
        }



    }

}
