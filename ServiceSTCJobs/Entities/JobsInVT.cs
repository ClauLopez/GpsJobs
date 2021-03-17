using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServiceSTCJobs.Entities
{
    public class JobsInVT
    {
        public string id { get; set; }
        public string description { get; set; }
        public string scheduledTime { get; set; }
        public string workerId { get; set; }//Es el userId
        public string routeId { get; set; }
        public int IdVehicleSTC { get; set; }

    }
}
