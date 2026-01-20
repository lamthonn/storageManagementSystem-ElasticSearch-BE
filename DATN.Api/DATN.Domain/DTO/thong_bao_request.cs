using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DATN.Domain.DTO
{
    public class thong_bao_request : BaseAuditableDto
    {
        public Guid id { get; set; }
        public Guid tai_lieu_id { get; set; }
        public string? tieu_de { get; set; }
        public string? noi_dung { get; set; }
        public string? nguoi_gui { get; set; }
        public string? nguoi_nhan { get; set; }
        public DateTime? ngay_gui { get; set; }
        public bool? da_xem { get; set; } = false;
    }

    public class respone_thong_bao_dto
    {
        public Guid thong_bao_id { get; set; }
    }
}
