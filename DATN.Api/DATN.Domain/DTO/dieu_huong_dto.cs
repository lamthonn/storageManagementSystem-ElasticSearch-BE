
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DATN.Domain.DTO
{
    public class dieu_huong_dto
    {
        public Guid Id { get; set; }
        public string? ma { get; set; }
        public string? ten { get; set; }
        public string? duong_dan { get; set; }
        public int? so_thu_tu { get; set; }
        public string? mo_ta { get; set; }
        public int? cap_dieu_huong { get; set; }
        public Guid? dieu_huong_cap_tren_id { get; set; }
        public List<dieu_huong_dto>? danh_sach_dieu_huong_con { get; set; } = new List<dieu_huong_dto>();
        public List<command_dto>? permission { get; set; }
    }
}
