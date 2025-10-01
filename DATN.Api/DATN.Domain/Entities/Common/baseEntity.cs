using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DATN.Domain.Entities.Common
{
    public abstract class BaseAuditableEntity
    {
        public DateTime? ngay_tao { get; set; }

        public string? nguoi_tao { get; set; }

        public DateTime? ngay_chinh_sua { get; set; }

        public string? nguoi_chinh_sua { get; set; }

        //[DefaultValue(false)]
        //public bool? IsDeleted { get; set; }  // Cờ đánh dấu xóa mềm (mặc định là false)
    }
}
