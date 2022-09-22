using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

#nullable disable

namespace ConsoleApp1.TargetModels
{
    [Table("HTRANSFER")]
    public partial class HTRANSFER
    {
        [Key]
        [StringLength(64)]
        public string ID { get; set; }
        [Required]
        [StringLength(64)]
        public string CARRIER_ID { get; set; }
        [StringLength(64)]
        public string LOT_ID { get; set; }
        public int TRANSFERSTATE { get; set; }
        public int COMMANDSTATE { get; set; }
        [StringLength(64)]
        public string HOSTSOURCE { get; set; }
        [Required]
        [StringLength(64)]
        public string HOSTDESTINATION { get; set; }
        public int PRIORITY { get; set; }
        [Required]
        [StringLength(2)]
        public string CHECKCODE { get; set; }
        [Required]
        [StringLength(1)]
        public string PAUSEFLAG { get; set; }
        [Key]
        public DateTime CMD_INSER_TIME { get; set; }
        public DateTime? CMD_START_TIME { get; set; }
        public DateTime? CMD_FINISH_TIME { get; set; }
        public int TIME_PRIORITY { get; set; }
        public int PORT_PRIORITY { get; set; }
        public int REPLACE { get; set; }
        public int PRIORITY_SUM { get; set; }
        [StringLength(64)]
        public string EXCUTE_CMD_ID { get; set; }
        [StringLength(2)]
        public string RESULT_CODE { get; set; }
        public int EXE_TIME { get; set; }
        public DateTime? T_STEMP { get; set; }
        [StringLength(64)]
        public string ID_TIME { get; set; }
    }
}
