using FTM.Common.Entities;
using System.ComponentModel;

namespace FTMPlus.Entities
{






    public class FlowProcess
    {


        public ProccesType ProccesType { get; set; }
        public string SourceServerCode { get; set; }
        //public string SourceDir { get; set; }
        //public string SoruceFilePatern { get; set; }
        //public string SourceFileFullPath { get; set; } 

        public List<FileMeta> SourceFiles { get; set; }
        public int Order { get; set; }

        public string TargetServerCode { get; set; }
        //public string TargetDir { get; set; }
        //public string TargetFilePatern { get; set; }
        //public string TargetFileFullPath { get; set; } 


        public string RowStatus { get; set; }
        public int ProcessId { get; set; }
        public int FlowId { get; set; }
        public int ProcessOrder { get; set; }
        public string ProcessName { get; set; }
        public string ProcessType { get; set; }
        public string IsActive { get; set; }
        public string IsArchive { get; set; } 
        public int SourcePort { get; set; } 
        public string SourceFileName { get; set; }
        public string SourceExtraFilters { get; set; }
        public string SourceDirectoryName { get; set; }
        public string SourceResultAction { get; set; }
        public string SourceNewFileExtension { get; set; }
        public bool SourceReadExchangeCredentialsFromConfig { get; set; }    
        public string TargetDomain { get; set; }
        public string TargetFileName { get; set; }
        public string TargetDirectoryName { get; set; }
        public bool TargetReadExchangeCredentialsFromConfig { get; set; }
        public bool MultipleFiles { get; set; }
        public bool CompressTargetFile { get; set; }
        public bool DecompressTargetFile { get; set; }
        public bool SendTargetFileAsAttachment { get; set; } 
        public bool WinscpEnabled { get; set; }
        public string IsOnErrorProcess { get; set; }
        public int TransferMode { get; set; }
        public string SourceHeaderIndicator { get; set; }
        public string SourceHeaderFilterRegex { get; set; }
        public string ErrorMoveDirectoryPath { get; set; }
        public string ValidationExpression { get; set; }
        public string NotificationTo { get; set; }
        public string MailTo { get; set; }
        public string MailSubject { get; set; }
        public string MailBody { get; set; }
        public string NotificationType { get; set; }
        public string NotificationWsUrl { get; set; }
        public string AssociationCode { get; set; }
        public string SystemIndex { get; set; }
        public string FileName { get; set; }
        public string FileFormatType { get; set; }
        public string IdExpression { get; set; }
    }

    public class Flow
    {



        public Guid Id { get; set; }
        public int FlowId { get; set; }
        public string FlowName { get; set; }
        public string FlowDescription { get; set; }
        public string FlowType { get; set; }
        public string FlowStatus { get; set; }
        public List<FlowProcess> Procces { get; set; }

        public int ProccessOrder { get; set; } = 1;

        //public List<FlowProcess> Process { get; set; }
        public List<object> FlowScheduleList { get; set; } 
        public string Description { get; set; }
        public int ExecutionInterval { get; set; }
        public string IsActive { get; set; }
        public string RecordStatus { get; set; } 
        public string AssociationCode { get; set; }
        public string UpdatingUser { get; set; }
        public DateTime UpdateDate { get; set; }
        public string Details { get; set; }
        public string IsTemplate { get; set; }
        public bool IsTryToReProceed { get; set; }
        public string ProcessFileOrderType { get; set; }
        public string ErrorMail { get; set; }
        public string FlowFileType { get; set; }
        public string IgnoreOldFiles { get; set; }
        public string FileReaderEncoding { get; set; }
        public string FileWriterEncoding { get; set; }
        public string OperatingSystemFileType { get; set; }
        public int TryCount { get; set; }
    }



     
    public enum ProccesType
    {
        [Description("")]
         Copy=1,
        [Description("Transform")]
        Transform=2,
        [Description("Merge")]
        Merge=3,
        [Description("Notify")]
        Notify=4,
        [Description("Check")]
        Check=5
    }
     
    public class FileServer
    {
        public string ServerCode { get; set; }
        public string ServerName { get; set; }
        public ServerType ServerType { get; set; }
        public string ServerStatus { get; set; }
        public string ServerIp { get; set; }
        public int ServerPort { get; set; }
        public string ServerUser { get; set; }
        public string ServerPassword { get; set; }
    }

    public enum ServerType
    {
        Filesystem = 1,
        FTP = 2,
        SFTP = 3,
        S3 = 4,
        Mail = 5
    }
}
