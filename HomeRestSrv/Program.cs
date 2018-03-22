using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel.Description;
using System.ServiceModel.Web;
using System.Text;
using System.Threading.Tasks;
using System.ServiceModel;

namespace HomeRestSrv
{
    class Program
    {
        static void Main(string[] args)
        {
            WebServiceHost host = new WebServiceHost(typeof(HomeServiceImp), new Uri("http://localhost:8000/home/"));

            var webBinding = new WebHttpBinding();
            ServiceEndpoint se = host.AddServiceEndpoint(typeof(IWebFile), webBinding, "");
            se = host.AddServiceEndpoint(typeof(IHomeService), webBinding, "rest");
            host.Open();
            Console.WriteLine("Hit any key to terminate");
            Console.ReadKey();
            host.Close();
        }
    }
}
