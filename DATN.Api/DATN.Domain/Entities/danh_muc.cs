using DATN.Domain.Entities.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DATN.Domain.Entities
{
    public class danh_muc : BaseAuditableEntity
    {
        public Guid Id { get; set; }
        [StringLength(255)]
        public string ma_dinh_danh { get; set; }
        [StringLength(255)]
        public string ma { get; set; }
        [StringLength(255)]
        public string ten { get; set; }
        public string? mo_ta { get; set; }
        //thêm mới
        [DefaultValue(false)]
        public bool trang_thai { get; set; } // 1: Đang hoạt động, 2: Không hoạt động , ....
        public virtual ICollection<nguoi_dung_2_danh_muc>? ds_nguoi_dung { get; set; }

    }
}
