using backend_v3.Dto.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DATN.Domain.DTO
{
    public class nhom_nguoi_dung_dto : PaginParams
    {
        public Guid? Id { get; set; }
        public string? ma { get; set; }
        public string? ten { get; set; }
        public string? mo_ta { get; set; }
        public int? trang_thai { get; set; }
        public int? cap_do { get; set; }
        public bool? isMacDinh { get; set; }
        public List<dieu_huong_dto>? lstDieuHuongs { get; set; }
        public List<nguoi_dung_dto>? lstNguoiDungs { get; set; }
    }
}
