using DATN.Domain.Entities.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DATN.Domain.Entities
{
    public class dieu_huong : BaseAuditableEntity
    {
        public Guid Id { get; set; }
        [StringLength(255)]
        public string ma { get; set; }
        [StringLength(255)]
        public string ten { get; set; }
        [StringLength(255)]
        public string duong_dan { get; set; }
        public int so_thu_tu { get; set; }
        public string? mo_ta { get; set; }
        public int cap_dieu_huong { get; set; }
        public Guid? dieu_huong_cap_tren_id { get; set; }
        public virtual ICollection<nhom_nguoi_dung_2_dieu_huong> ds_nhom_nguoi_dung { get; set; }
        public virtual ICollection<nhom_nguoi_dung_2_command> ds_nhom_nguoi_dung_2_command { get; set; }
        public virtual ICollection<nhat_ky_he_thong> ds_nhat_ky_he_thong { get; set; }
        public virtual ICollection<dieu_huong_2_command> ds_dieu_huong_2_command { get; set; }
    }
}
