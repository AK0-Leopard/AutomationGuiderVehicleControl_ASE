using com.mirle.ibg3k0.sc.App;
using com.mirle.ibg3k0.sc.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace com.mirle.ibg3k0.sc.BLL
{
    public class EqptBLL
    {
        public DB OperateDB { private set; get; }
        public Catch OperateCatch { private set; get; }

        public EqptBLL()
        {
        }
        public void start(SCApplication _app)
        {
            OperateDB = new DB();
            OperateCatch = new Catch(_app.getEQObjCacheManager());
        }
        public class DB
        {

        }
        public class Catch
        {
            EQObjCacheManager CacheManager;
            public Catch(EQObjCacheManager _cache_manager)
            {
                CacheManager = _cache_manager;
            }

            public AEQPT GetEqpt(string eqptID)
            {
                var eqpt = CacheManager.getAllEquipment().
                             Where(u => SCUtility.isMatche(u.EQPT_ID, eqptID)).
                             SingleOrDefault();
                return eqpt;
            }
            public List<AGVStation> loadAllAGVStation()
            {
                var eqpts = CacheManager.getAllEquipment().
                             Where(e => e is AGVStation).
                             Select(e => e as AGVStation).
                             ToList();
                return eqpts;
            }
            public AGVStation getAGVStation(string portID)
            {
                var eqpt = CacheManager.getAllEquipment().
                             Where(e => (e is AGVStation) &&
                                        SCUtility.isMatche((e as AGVStation).EQPT_ID, portID)).
                             Select(e => e as AGVStation).
                             FirstOrDefault();
                return eqpt;
            }

            public bool IsAGVStation(string portID)
            {
                var eqpt = CacheManager.getAllEquipment().
                             Where(e => (e is AGVStation) &&
                                        SCUtility.isMatche((e as AGVStation).EQPT_ID, portID)).
                             FirstOrDefault();
                return eqpt != null;
            }

            public SCAppConstants.EqptType GetEqptType(string eqptID)
            {
                var eqpt = CacheManager.getAllEquipment().
                             Where(u => SCUtility.isMatche(u.EQPT_ID, eqptID)).
                             SingleOrDefault();
                return eqpt.Type;
            }
        }
    }
}