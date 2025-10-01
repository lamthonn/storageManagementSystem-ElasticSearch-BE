using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DATN.Domain.DTO
{
    public class menu_dto
    {
        public string? key { get; set; }
        public string? label { get; set; }
        public string? ma_dinh_danh { get; set; }
        public List<string>? Permissions { get; set; }
        public List<menu_dto>? children { get; set; }
    }
}
