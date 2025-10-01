using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DATN.Domain.Entities
{
    public class nhom_nguoi_dung_2_dieu_huong
    {
        [Key]
        public Guid id { get; set; }
        public Guid dieu_huong_id { get; set; }
        public Guid nhom_nguoi_dung_id { get; set; }
        [ForeignKey(nameof(nhom_nguoi_dung_id))]
        public virtual nhom_nguoi_dung nhom_nguoi_dung { get; set; }
        [ForeignKey(nameof(dieu_huong_id))]
        public virtual dieu_huong dieu_huong { get; set; }
    }
}
