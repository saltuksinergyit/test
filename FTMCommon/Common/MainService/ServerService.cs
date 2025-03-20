using FTMPlus.Entities;

namespace FTMPlus.Common.MainService
{
    public class ServerService
    {
        public FileServer GetFileServer(string serverCode)
        {

            switch (serverCode)
            {
                case "1":
                    return new FileServer() { ServerCode = "1", ServerIp = "nas.in.sinergyit.com", ServerPort = 22, ServerName = "", ServerUser = "sspc", ServerPassword = "1", ServerType = ServerType.SFTP };
                case "2":
                    return new FileServer() { ServerCode = "2", ServerIp = "http://127.0.0.1:9000", ServerPort = 0, ServerName = "bucks1", ServerUser = "A5dHLYr2x7szNCREb9g6", ServerPassword = "BESytAkKm82Aflqt4ekRc272J5BD2em0fXcmQx9e", ServerType = ServerType.S3 };
                case "3":
                    return new FileServer() { ServerCode = "3", ServerIp = "/", ServerPort = 0, ServerName = "", ServerUser = "", ServerPassword = "BESytAkKm82Aflqt4ekRc272J5BD2em0fXcmQx9e", ServerType = ServerType.Filesystem };
                case "4":
                    return new FileServer() { ServerCode = "4", ServerIp = "127.0.0.1", ServerPort = 21, ServerName = "", ServerUser = "g1", ServerPassword = "1", ServerType = ServerType.FTP };
                default:
                    break;
            }

            return new FileServer() { ServerCode = "1", ServerIp = "nas.in.sinergyit.com", ServerPort = 22, ServerName = "", ServerUser = "sspc", ServerPassword = "1", ServerType = ServerType.SFTP }; 
        }
    }
}
