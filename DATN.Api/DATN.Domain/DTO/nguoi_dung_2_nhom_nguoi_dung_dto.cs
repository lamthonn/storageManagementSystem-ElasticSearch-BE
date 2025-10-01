using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DATN.Domain.DTO
{
    public class nguoi_dung_2_nhom_nguoi_dung_dto
    {
        public Guid? id { get; set; }
        public Guid? nguoi_dung_id { get; set; }
        public Guid? nhom_nguoi_dung_id { get; set; }
        public string? ten { get; set; }
        public string? ma { get; set; }
        public bool mac_dinh { get; set; }
    }
}
