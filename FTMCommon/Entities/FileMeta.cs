using FTMPlus.Entities;
using System.Text.Json.Serialization;

namespace FTM.Common.Entities
{
    public class FileMeta
    {
        /// <summary>
        /// Dosyanın tam yolu (Path + Name)
        /// </summary>
        public string FullPath { get; set; }
        /// <summary>
        /// Dosyanın tam yolu (Path + Name)
        /// </summary>
        public string? DestinationPath { get; set; } = null;

        /// <summary>
        /// Dosya adı
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// Dosya boyutu (byte)
        /// </summary>
        public long Size { get; set; }

        /// <summary>
        /// Dosyanın oluşturulma tarihi
        /// </summary>
        public DateTime CreatedDate { get; set; }

        /// <summary>
        /// Dosyanın Düzenleme tarihi
        /// </summary>
        public DateTime UpdateDate { get; set; }

        /// <summary>
        /// Dosyanın provider tipi (Filesystem, FTP, SFTP, S3, Email vs.)
        /// </summary>
        public SourceType SourceType { get; set; }

        /// <summary>
        /// Dosyanın son işlenme durumu
        /// </summary>
        public ProcessingStatus Status { get; set; }
        public string ErrorText { get; set; }
        [JsonIgnore]
        public object OrginalData { get; set; }

        public string ServerCode { get; set; }
        public string Id { get; set; }
        public string lockKey { get; set; }
    }

    public enum ProcessingStatus
    {
        /// <summary>
        /// Yeni eklendi, henüz işlenmedi.
        /// </summary>
        New,

        /// <summary>
        /// İşleniyor
        /// </summary>
        Processing,
        /// <summary>
        /// İşlendi
        /// </summary>
        Processed,
        /// <summary>
        /// Başarılı şekilde işlendi
        /// Processed status db yazar
        /// </summary>
        Completed,

        /// <summary>
        /// Hata aldı
        /// 
        /// </summary>
        Failed
    }
 
    public enum SourceType
    {
        /// <summary>
        /// Yerel dosya sistemi (C:\Temp\ gibi)
        /// </summary>
        Filesystem = 0,

        /// <summary>
        /// FTP sunucusu
        /// </summary>
        FTP = 1,

        /// <summary>
        /// SFTP sunucusu (Secure FTP)
        /// </summary>
        SFTP = 2,

        /// <summary>
        /// Amazon S3 ya da S3 uyumlu object storage
        /// </summary>
        S3 = 3,

        /// <summary>
        /// E-posta kutusu (IMAP/POP3)
        /// </summary>
        Email = 4,

        /// <summary>
        /// Başka bir kaynak eklenecekse buradan genişletilebilir
        /// </summary>
        Other = 99
    }



    public class FlowsChunks
    {
        public string ServerCode { get; set; }
        public List<Flow> Flows { get; set; }
    }
}
