using MySql.Data.MySqlClient;
using ServiceSTCJobs.Entities;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServiceSTCJobs.DataBase
{

    public class StcDataBase
    {
        NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
        string STCConnection = string.Empty;
         public StcDataBase()
        {
            STCConnection = System.Configuration.ConfigurationManager.ConnectionStrings["StcBD"].ToString();
        }
        /// <summary>
        ///Realiza una consulta a la DB de STC para obtener todas las unidades que tenegan userid de VT
        /// </summary>
        /// <returns></returns>
        public List<Vehicle> GetVehicleList()
        {

            List<Vehicle> obJClientes = new List<Vehicle>();
            using (MySqlConnection conn = new MySqlConnection(STCConnection))
            {
                try
                {
                    logger.Info("Inicia consulta para obtener los vehiclulos de la DB de STC");
                    conn.Open();
                    String CommandToExecute = string.Format("{0}{1}{2}{3}{4}",
                        "SELECT V.Plate, V.UserIdVT, CG.VTServerName, CG.VTAppId, V.Id ",
                        "FROM Vehicles V ",
                        "JOIN CediCCs CC on V.Vehicle_Cedi = CC.Id ",
                        "JOIN ClientGroups CG on CC.ClientGroup_CediCC = CG.Id ",
                        "WHERE V.UserIdVT IS NOT NULL; ");
                    using (MySqlCommand command = new MySqlCommand(CommandToExecute, conn))
                    {
                        command.CommandTimeout = 0;
                        using (MySqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                try
                                {
                                    Vehicle objVeh = new Vehicle();
                                    objVeh.Plate = reader.GetString(0);
                                    objVeh.UserId = reader.GetInt32(1);
                                    objVeh.VtServer = reader.GetString(2);
                                    objVeh.AppId = reader.GetInt32(3);
                                    objVeh.IdVehicleSTC = reader.GetInt32(4);

                                    obJClientes.Add(objVeh);
                                }
                                catch (Exception ex)
                                {
                                    logger.Error(string.Format("JobsER001-1: Error al obtener los datos de DB-STC {0}", ex.Message));
                                }
                            }
                        }
                    }
                }
                catch (MySqlException ex)
                {
                    logger.Error(string.Format("JobsER001: Error al conectarse a DB-STC {0}", ex.Message));
                }
                finally
                {
                    if (conn.State == System.Data.ConnectionState.Open)
                        conn.Close();
                }
            }
            logger.Trace("Termina de obtener los vehiculos, sale con {0} unidades", obJClientes.Count);

            return obJClientes;
        }
        /// <summary>
        /// Busca los Jobs existentes en DB
        /// </summary>
        /// <param name="CommandToExecute"></param>
        public List<string> ValidateExistJobs(string CommandToExecute)
        {
            List<string> JobsInDB = new List<string>();
            logger.Info("Inicia proceso de busqueda de Jobs existentes en VT");

            using (MySqlConnection conn = new MySqlConnection(STCConnection))
            {
                try
                {
                    conn.Open();

                    using (MySqlCommand command = new MySqlCommand(CommandToExecute.ToString(), conn))
                    {
                        command.CommandTimeout = 0;
                        using (MySqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                JobsInDB.Add(reader.GetString(0));
                            }
                        }
                    }
                    logger.Trace("Terminando de buscar jobs");
                }
                catch (MySqlException ex)
                {
                    logger.Error(string.Format("JobsER003: Error al conectarse a DB-STC {0}", ex.Message));
                }
                finally
                {
                    if (conn.State == System.Data.ConnectionState.Open)
                        conn.Close();
                }
            }
            return JobsInDB;
        }

        /// <summary>
        /// Guarda los jobs que se encontraron en VT, en la tabla de STC 
        /// </summary>
        /// <param name="CommandToExecute"></param>
        internal void SaveJobsToDB(StringBuilder CommandToExecute)
        {
            logger.Info("Inicia proceso de insercion de Jobs encontrados en VT");

            using (MySqlConnection conn = new MySqlConnection(STCConnection))
            {
                try
                {
                    conn.Open();

                    using (MySqlCommand command = new MySqlCommand(CommandToExecute.ToString(), conn))
                    {
                        command.CommandTimeout = 0;
                        using (MySqlDataReader reader = command.ExecuteReader())
                        {

                            while (reader.Read())
                            {
                            }
                        }
                    }
                    logger.Trace("Jobs agregadon si problema");
                }
                catch (MySqlException ex)
                {
                    logger.Error(string.Format("JobsER002: Error al conectarse a DB-STC {0}", ex.Message));
                }
                finally
                {
                    if (conn.State == System.Data.ConnectionState.Open)
                        conn.Close();
                }
            }
        }
    }
}
