using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.IO;

namespace HomeRestSrv
{   
    [DataContract]
    public class HistoryRecord
    {
        [DataMember]
        public string timestamp;
        [DataMember]
        public string id;
        [DataMember]
        public string name;
        [DataMember]
        public int state;
        public HistoryRecord(string _id, string _name, long timestampTicks, int _state)
        {
            id = _id;
            name = _name;
            state = _state;
            timestamp = new DateTime(timestampTicks).ToString("yyyy-MM-dd HH:mm:ss.ffffff");
        }
    }

    [DataContract]
    public class Control
    {
        [DataMember]
        public string id;
        [DataMember]
        public string name;
        [DataMember]
        public int fileNumber;
        [DataMember]
        public int element;
        [DataMember]
        public int subElement;
        [DataMember]
        public string fileType;
        [DataMember]
        public int offset;

        public Control(string _id, string _name , int _fileNumber, int _element,
                        int _subElement, string _fileType, int _offset)
        {
            id = _id;
            name = _name;
            fileNumber = _fileNumber;
            element = _element;
            subElement = _subElement;
            fileType = _fileType;
            offset = _offset;
        }

    }
    [ServiceContract]
    public interface IHomeService
    {
        // rest/history?startTime=2017-09-15T14:00:23.1234&lines=25
        [OperationContract]
        [WebGet(ResponseFormat = WebMessageFormat.Json)]
        HistoryRecord[] History(string name, string startTime, uint lines);

        [OperationContract]
        [WebGet(ResponseFormat = WebMessageFormat.Json)]
        Control[] Controls();
    }

    [ServiceContract]
    public interface IWebFile
    {
        [OperationContract]
        [WebGet(BodyStyle = WebMessageBodyStyle.Bare, UriTemplate = "*")]

        Stream GetPage();
    }
}
    