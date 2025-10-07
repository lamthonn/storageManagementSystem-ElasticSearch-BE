using DATN.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DATN.Domain.DTO
{
    public class thu_muc_dto : BaseAuditableDto
    {
        public Guid? id { get; set; }
        public string? ten { get; set; }
        public Guid? thu_muc_cha_id { get; set; }
        public Guid? nguoi_dung_id { get; set; }
        public List<tai_lieu_dto>? ds_tai_lieu { get; set; }
    }
}
