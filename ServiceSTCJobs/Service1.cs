using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceSTCJobs
{
    public partial class Service1 : ServiceBase
    {
        public Service1()
        {
            InitializeComponent();
        }

        NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
        int TimerToRestartProcess = Convert.ToInt32(ConfigurationManager.AppSettings["TimerToRestartProcess"]);

        private void DoWork(object arg)
        {
            for (; ; )
            {

                //Para probar:
                //1) Rebuild a este proyecto 
                //2) Inicia CMD as admin;
                //3) escribe en CMD cd carpetaenCconcompilado
                //4) escribe toda esta linea:
                //CD C:\Windows\Microsoft.NET\Framework\v4.0.30319\InstallUtil.exe ServiceSTCGpscommunication.exe
                //Para el setup borra resultadoprincipal seleccionando este proyecto 
                logger.Info("Inicia procesos del servicio ServiceSTCJobs [V-1.0.3]");

                STCSearchJobs obj = new STCSearchJobs();
                obj.STCprocessJobsInUser();

                // Ejecuta este codigo una vez cada X tiempo y lo detiene por X minutos
                logger.Info(string.Format("Esperando {0} ms para iniciar el proceso nuevamente...", TimerToRestartProcess));
                if (StopRequest.WaitOne(TimerToRestartProcess)) return;

            }
        }

        Thread Worker;
        AutoResetEvent StopRequest = new AutoResetEvent(false);

        protected override void OnStart(string[] args)
        {
            try
            {
                Worker = new Thread(DoWork);
                Worker.Start();
               
            }
            catch (Exception ex)
            {
                logger.Error("ServiceSTCJobs ERROR: {0}", ex.Message);
            }
        }

        protected override void OnStop()
        {
            try
            {
                logger.Info("Terminando procesos del servicio ServiceSTCJobs [V-1.0.3]");
                StopRequest.Set();
                Worker.Join();
            }
            catch (Exception ex)
            {
                logger.Error("ServiceSTCJobs ERROR al detener servicio : {0}", ex.Message);
            }
        }
    }
}
