using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using backend_v3.Dto.Common;

namespace DATN.Domain.DTO
{
    public class danh_muc_dto : PaginParams
    {
        public Guid Id { get; set; }
        public string? ma_dinh_danh { get; set; }
        public string? ma { get; set; }
        public string? ten { get; set; }
        public string? mo_ta { get; set; }
        public bool? trang_thai { get; set; }
    }
}
