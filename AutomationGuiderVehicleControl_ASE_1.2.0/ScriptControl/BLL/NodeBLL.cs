﻿using com.mirle.ibg3k0.sc.App;
using com.mirle.ibg3k0.sc.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace com.mirle.ibg3k0.sc.BLL
{
    public class NodeBLL
    {
        public DB OperateDB { private set; get; }
        public Catch OperateCatch { private set; get; }

        public NodeBLL()
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
            public List<ANODE> loadLCSNodes()
            {
                var nodes = CacheManager.getAllNode();
                var lcs_nodes = nodes.Where(n => !SCUtility.isEmpty(n.TcpIpAgentName)).ToList();
                return lcs_nodes;
            }

        }
    }
}