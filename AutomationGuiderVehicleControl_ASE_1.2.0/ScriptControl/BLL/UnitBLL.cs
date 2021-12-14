
using com.mirle.ibg3k0.sc.App;
using com.mirle.ibg3k0.sc.Common;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace com.mirle.ibg3k0.sc.BLL
{
    public class UnitBLL
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();

        public DB OperateDB { private set; get; }
        public Catch OperateCatch { private set; get; }
        public Web web { get; private set; }

        public UnitBLL()
        {
        }
        public void start(SCApplication _app)
        {
            OperateDB = new DB();
            OperateCatch = new Catch(_app.getEQObjCacheManager());
            web = new Web(_app.webClientManager);
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

            public AUNIT getUnit(string unitID)
            {
                AUNIT unit = CacheManager.getAllUnit().
                             Where(u => SCUtility.isMatche(u.UNIT_ID, unitID)).
                             SingleOrDefault();
                return unit;
            }
            public List<AUNIT> loadUnits()
            {
                List<AUNIT> units = CacheManager.getAllUnit().
                             Where(u => u.UNIT_ID.Contains("Charger")).
                             ToList();
                return units;
            }
        }
        public class Web
        {
            WebClientManager webClientManager = null;
            List<string> notify_urls = new List<string>()
            {
                //"http://stk01.asek21.mirle.com.tw:15000",
                 "http://agvc.asek21.mirle.com.tw:15000"
            };
            const string CAHRGE_STATUS_IS_ABNORMAL_CONST = "ChargeStatusIsAbnormal";

            public Web(WebClientManager _webClient)
            {
                webClientManager = _webClient;
            }

            public void ChargerStatusIsAbnormal()
            {
                try
                {
                    string[] action_targets = new string[]
                    {
                    "weatherforecast"
                    };
                    string[] param = new string[]
                    {
                    CAHRGE_STATUS_IS_ABNORMAL_CONST,
                    };
                    foreach (string notify_url in notify_urls)
                    {
                        string result = webClientManager.GetInfoFromServer(notify_url, action_targets, param);
                    }
                }
                catch (Exception ex)
                {
                    logger.Error(ex, "Exception");
                }
            }


        }
    }
}