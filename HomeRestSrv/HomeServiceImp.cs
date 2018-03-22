using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Globalization;
using System.Threading;
using System.ServiceModel.Web;
using System.IO;
using MimeTypes;

namespace HomeRestSrv
{

    public class HomeServiceImp : IHomeService, IWebFile
    {
        public static DBUtil DbUtil { get; set; } = new DBUtil();
        public HomeServiceImp()
        { }

        public HistoryRecord[] History(string name, string startTime, uint lines)
        {


            const string _dateFormat = @"yyyy-MM-dd HH:mm:ss.ffffff";
            DateTime date = new DateTime();

            if ((null == startTime) ||
                !DateTime.TryParseExact(startTime, _dateFormat, Thread.CurrentThread.CurrentCulture, DateTimeStyles.None, out date))
            {
                Console.WriteLine("Date could not parse.");
                date = DateTime.Now;
            }

            var Recordlist = DbUtil.DeviceHistory(name, date.Ticks, lines);
            return Recordlist.ToArray();
        }

        public Control[] Controls()
        {
            var controlsList = DbUtil.GetControl();
            return controlsList.ToArray();
        }

        public Stream GetPage()
        {

            var ir = WebOperationContext.Current.IncomingRequest;
            var or = WebOperationContext.Current.OutgoingResponse;

            var root = System.Configuration.ConfigurationManager.AppSettings["webRoot"];

            var filePath = String.Join("/", ir.UriTemplateMatch.WildcardPathSegments.ToArray());
            if (String.IsNullOrWhiteSpace(filePath))
                filePath = "index.html";
            if (filePath.Contains(".."))
            {
                or.StatusCode = System.Net.HttpStatusCode.BadRequest;
                or.ContentType = "text/plain";
                return new MemoryStream(Encoding.UTF8.GetBytes("Shame On you"));
            }

            var path = Path.Combine(root, filePath);

            if (!File.Exists(path))
            {
                or.StatusCode = System.Net.HttpStatusCode.NotFound;
                or.ContentType = "text/plain";
                return new MemoryStream(Encoding.UTF8.GetBytes("Page Not Found"));
            }

            var pageStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            var cType = MimeTypes.MimeTypeMap.GetMimeType(Path.GetExtension(path));
            or.ContentType = cType;

            return pageStream;
        }
    }
}
