using DATN.Domain.Entities;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using backend_v3.Dto.Common;

namespace DATN.Domain.DTO
{
    public class nhat_ky_he_thong_dto
    {
        public Guid? id { get; set; }
        public string? command { get; set; }
        public string? tai_khoan { get; set; }
        public string? ten { get; set; }
        public Guid? command_id { get; set; }
        public Guid? dieu_huong_id { get; set; }
        public int? loai { get; set; } // infor || error || warning || debug
        public int? level { get; set; }
        public DateTime? TimeStamp { get; set; }

        public string? detail { get; set; }
    }

    public class nhat_ky_he_thong_pagin_param : PaginParams
    {
        public string? command { get; set; }
        public string? tai_khoan { get; set; }
        public string? ten { get; set; }
        public string? detail { get; set; }
        public DateTime? TimeStamp { get; set; }
        public int? loai { get; set; } // infor || error || warning || debug
         
    }
}
