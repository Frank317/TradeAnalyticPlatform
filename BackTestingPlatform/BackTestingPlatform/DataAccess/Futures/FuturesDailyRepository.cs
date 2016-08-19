﻿using BackTestingPlatform.DataAccess.Common;
using BackTestingPlatform.Model.Futures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BackTestingPlatform.DataAccess.Futures
{
    public class FuturesDailyRepository : BasicDataRepository<FuturesDaily>
    {
        protected override List<FuturesDaily> readFromWind()
        {
            throw new NotImplementedException();
        }
    }
}