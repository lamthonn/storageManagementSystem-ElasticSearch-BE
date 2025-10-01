using DATN.Domain.Entities.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DATN.Domain.Entities
{
    public class nhom_nguoi_dung : BaseAuditableEntity
    {
        public Guid Id { get; set; }
        public string ma { get; set; }
        public string ten { get; set; }
        public string? mo_ta { get; set; }
        public int trang_thai { get; set; }
        public virtual ICollection<nguoi_dung_2_nhom_nguoi_dung> ds_nguoi_dung { get; set; }
        public virtual ICollection<nhom_nguoi_dung_2_dieu_huong> ds_dieu_huong { get; set; }
        public virtual ICollection<nhom_nguoi_dung_2_command> ds_dm_command { get; set; }
    }
}
