using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServiceSTCJobs.Entities
{
    public class Vehicle
    {
        public int IdVehicleSTC { get; set; }
        public string Plate { get; set; }
        public int UserId { get; set; }
        public string VtServer { get; set; }
        public int AppId { get; set; }
    }
}
