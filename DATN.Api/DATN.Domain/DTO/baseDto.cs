using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DATN.Domain.DTO
{
    public class BaseAuditableDto
    {
        public DateTime? ngay_tao { get; set; }
        public DateTime? fromDate { get; set; }
        public DateTime? toDate { get; set; }

        public string? nguoi_tao { get; set; }

        public DateTime? ngay_chinh_sua { get; set; }

        public string? nguoi_chinh_sua { get; set; }
    }
}
