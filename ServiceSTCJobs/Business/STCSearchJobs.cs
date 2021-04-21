using Newtonsoft.Json;
using ServiceSTCJobs.DataBase;
using ServiceSTCJobs.Entities;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace ServiceSTCJobs
{
    public class STCSearchJobs
    {

        NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
        /// <summary>
        /// Realiza la busqueda de los jobs de cada unidad 
        /// </summary>
        /// <returns></returns>
        public Boolean STCprocessJobsInUser()
        {
            Boolean Complete = false;
            try
            {
                //1) Obtiene la lista de vehiculos en DB STC que tienen userId de VT
                List<Vehicle> VehSTC = new List<Vehicle>();
                VehSTC = getSTCVehicles();

                int CuentaVeh = VehSTC.Count();
                int flg = 1;
                //Con cada unidad buscar en VT y guardar los jobs
                foreach (Vehicle Veh in VehSTC)
                {
                    logger.Trace("Procesando {0} de {1} unidades", flg, CuentaVeh);
                    List<JobsInVT> result = SearchUnitInVT(Veh.VtServer, Veh.AppId, Veh.UserId);
                    if (result != null && result.Count > 0)
                    {
                        List<JobsInVT> RouteNoExist = FilterRouteExist(result);

                        if (RouteNoExist != null && RouteNoExist.Count() > 0)
                        {
                            SaveResultToDB(RouteNoExist, Veh.IdVehicleSTC);
                        }
                    }
                    flg++;
                }
                logger.Trace("Terminando..., todas las unidades fueron procesadas.", flg - 1, CuentaVeh);
            }
            catch (Exception ex)
            {
                Complete = false;
                logger.Error("Error al obtener los jobs de las unidades que existen en STC {0}", ex.Message);
            }
            return Complete;
        }
        /// <summary>
        /// Filtra los jobs y rutas para identificar si existen en DB y no duplicar
        /// </summary>
        /// <param name="result"></param>
        /// <returns></returns>
        private List<JobsInVT> FilterRouteExist(List<JobsInVT> allJobs)
        {
            List<JobsInVT> FullList = new List<JobsInVT>();
            try
            {
                StringBuilder Sb = new StringBuilder();
                //Agrupa para sacar los id's de ruta
                List<string> RouteGroup = (from x in allJobs
                                           group x by x.routeId into DGroup
                                           orderby DGroup.Key
                                           select DGroup.Key).ToList();

                //se van a buscar los jobs de cada ruta 
                foreach (string Route in RouteGroup)
                {
                    List<JobsInVT> JobsInRoute = (from x in allJobs
                                                  where x.routeId.Equals(Route)
                                                  select x).ToList();
                    Sb.Append(string.Format("SELECT JobId FROM JobsInVt WHERE RouteId = '{0}' AND JobId IN (", Route));
                    foreach (JobsInVT Job in JobsInRoute)
                    {
                        Sb.Append(string.Format("'{0}',", Job.id));
                    }

                    Sb.Remove(Sb.Length - 1, 1);
                    Sb.Append(");");

                    StcDataBase DB = new StcDataBase();
                    //Va a buscar si estos jobs ya existen en la DB para no duplicarlos
                    List<string> JobsInDB = DB.ValidateExistJobs(Sb.ToString());
                    //Los jobs que trae la lista son los que si existen en la DB 
                    //Hay que filtrarlos a la lista completa para que no se dupliquen al hacer insert 
                    List<JobsInVT> allJobsFilter = (from x in allJobs
                                                    where x.routeId.Equals(Route) &&
                                                    !JobsInDB.Contains(x.id)
                                                    select x).ToList();
                    if (allJobsFilter.Count() > 0)
                    {
                        FullList.AddRange(allJobs);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error("Error al buscar jobs/rutas existentes en STC {0}", ex.Message);
            }
            return FullList;
        }

        /// <summary>
        /// Genera los inserts de cada job y los guarda en DB STC
        /// </summary>
        /// <param name="JobList">Lista de jobs que tiene la unidad en VT</param>
        /// <param name="IdVehicleSTC">Id de esta unidad en tabla vehiculos de STC</param>
        private void SaveResultToDB(List<JobsInVT> JobList, int IdVehicleSTC)
        {
            //Genera el insert de todos los jobs 
            //Guarda en DB  
            try
            {
                StringBuilder Sb = new StringBuilder();
                //Por cada Job genera un insert y lo guarda en el builder
                foreach (JobsInVT Job in JobList)
                {
                    Sb.Append(string.Format("{0}{1}{2}{3}",
                        "INSERT INTO  JobsInVt ",
                        "(`RouteName`,`ScheduledTime`,`RouteId`,`JobId`,`UserIdVT`,`IdVehicleSTC`,",
                        "`CreatedBy`,`Created`,`ModifiedBy`,`Modified`)",
                        string.Format(" VALUES('{0}','{1}','{2}','{3}',{4},{5},'JobsApp',UTC_TIMESTAMP(),'JobsApp',UTC_TIMESTAMP()); ",
                        Job.description.Replace("'", @"\'"), Job.scheduledTime, Job.routeId, Job.id, Job.workerId, IdVehicleSTC)));
                }

                StcDataBase DB = new StcDataBase();
                DB.SaveJobsToDB(Sb);
            }
            catch (Exception ex)
            {
                logger.Error("Error al consutar DB, cuando guarda los jobs {0}", ex.Message);
            }
        }

        /// <summary>
        /// Realiza el llamado a VT para obtener los jobs de la unidad
        /// </summary>
        /// <param name="userId"></param>
        private List<JobsInVT> SearchUnitInVT(string VtServer, int AppId, int userId)
        {
            List<JobsInVT> result = null;
            string URLTrackingVT = string.Empty;
            try
            {
                string pathToken = string.Format("/api/gps/GetJobsByUser");
                string parameters = string.Format("?TraceKey=getJobs&ServerVT={0}&AppIdVT={1}&User={2}", VtServer, AppId, userId);
                URLTrackingVT = string.Format("{0}{1}{2}", ConfigurationManager.AppSettings["serverSoGosmo"], pathToken, parameters);
                logger.Trace(string.Format("Inicia solicitud a VT de los Jobs de la unidad {0}, se manda: {1}", userId, URLTrackingVT));

                HttpWebRequest servicio = (HttpWebRequest)WebRequest.Create(new Uri(URLTrackingVT));
                servicio.Method = "POST";
                servicio.ContentType = "application/json; charset=utf-8";

                //Se mandan los parametros al body del servicio
                using (var stream = new StreamWriter(servicio.GetRequestStream()))
                {
                    stream.Write(JsonConvert.SerializeObject(parameters));
                    stream.Flush();
                    stream.Close();
                }

                //Se obtiene el response del REST
                var httpResponse = (HttpWebResponse)servicio.GetResponse();
                using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                {
                    try
                    {
                        string AllJobs = streamReader.ReadToEnd();
                        if (AllJobs.Equals(string.Empty) || AllJobs.Equals("[]") || AllJobs.Contains("error"))
                        {
                            logger.Trace("Sin Jobs para la unidad {0} {1}", VtServer, userId);
                        }
                        else
                        {
                            result = JsonConvert.DeserializeObject<List<JobsInVT>>(AllJobs);

                            logger.Trace("Recibiendo respuesta de API manager para la unidad {0} {1} Con: {2} registros", VtServer, userId, result.Count());
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.Error(string.Format("Error al recibir respuesta de VT {0}, URL: {1}", ex.Message, URLTrackingVT));
                    }
                }
            }
            catch (HttpRequestException re)
            {
                logger.Error(string.Format("Se genera un HttpRequestException al solicitar Jobs a VT {0}, URL: {1}", re.Message, URLTrackingVT));
            }
            catch (Exception ex)
            {
                if (ex.Message.ToLower().Contains("timed out") || ex.Message.ToLower().Contains("tiempo de espera"))
                {
                    logger.Info("Al consultar los jobs existentes en VT por Api Manager, regreso con un {0}, el llamado fue-->> {1} <<--, se reintentara posteriormente", ex.Message, URLTrackingVT);
                }
                else
                    logger.Error("Error al hacer llamado al Manager en seccion que va a REST de VT {0}", ex.Message);
            }

            return result;
        }

        /// <summary>
        /// Obtiene la lista de unidades existentes en STC
        /// </summary>
        /// <returns></returns>
        private List<Vehicle> getSTCVehicles()
        {
            List<Vehicle> Veh = null;
            try
            {
                StcDataBase DB = new StcDataBase();
                Veh = DB.GetVehicleList();
            }
            catch (Exception ex)
            {
                logger.Error("Error al consutar DB, cuando pide los vehiculos {0}", ex.Message);
            }
            return Veh;
        }
    }
}
