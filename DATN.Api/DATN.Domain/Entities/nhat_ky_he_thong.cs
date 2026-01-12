using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DATN.Domain.Entities
{
    public class nhat_ky_he_thong
    {
        [Key]
        public Guid id { get; set; }
        [Column(TypeName = "text")]
        public string? command { get; set; }
        [StringLength(255)]
        public string? tai_khoan { get; set; }
        public string? ten { get; set; }
        public Guid? command_id { get; set; }
        public Guid? dieu_huong_id { get; set; }
        public int? loai { get; set; } // 1 - infor || 2 - error || 3 - warning || 4 - debug
        public int? level { get; set; } 
        public DateTime? TimeStamp { get; set; }

        public string? detail { get; set; }
        [ForeignKey("command_id")]
        public virtual dm_command? dm_command { get; set; }

        [ForeignKey("dieu_huong_id")]
        public virtual dieu_huong? dieu_huong { get; set; }
    }
}
