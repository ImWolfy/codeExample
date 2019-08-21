using ApiMultiPartFormData;
using AutoCallerWindowsService.Global;
using System;
using System.ServiceProcess;
using System.Web.Http;
using System.Web.Http.SelfHost;

namespace AutoCallerWindowsService
{
    public partial class MedhelpAutoCallerService : ServiceBase
    {
        public MedhelpAutoCallerService()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            try
            {
                Settings settings = Settings.Instance;
                LogWriter.Add("Запуск службы");

                var config = new HttpSelfHostConfiguration($"http://{settings.AutoCallerAPIIPAddress}:{settings.AutoCallerAPIPort});
                config.Formatters.Add(new MultipartFormDataFormatter());
                config.MaxBufferSize = 10485760;
                config.MaxReceivedMessageSize = 10485760;
                config.Routes.MapHttpRoute("API Default", "{controller}/{action}/{param1}/{param2}/{param3}", new { param1 = RouteParameter.Optional, param2 = RouteParameter.Optional, param3 = RouteParameter.Optional });
                config.IncludeErrorDetailPolicy = IncludeErrorDetailPolicy.Always;
                var server = new HttpSelfHostServer(config);
                server.OpenAsync().Wait();
            }
            catch(Exception exc)
            {
                LogWriter.Add("Ошибка запуска службы. " + exc.Message);
            }
        }

        protected override void OnStop()
        {
            LogWriter.Add("Служба остановлена");
        }
    }
}
