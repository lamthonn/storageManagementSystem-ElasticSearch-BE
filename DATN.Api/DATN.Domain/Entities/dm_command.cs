using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DATN.Domain.Entities
{
    public class dm_command
    {
        [Key]
        public Guid id { get; set; }
        [StringLength(512)]
        public string command { get; set; }
        [StringLength(255)]
        public string ten { get; set; }
        public virtual ICollection<nhat_ky_he_thong> ds_system_log { get; set; }

        public virtual ICollection<dieu_huong_2_command>? ds_dieu_huong_2_command { get; set; }
    }
}
