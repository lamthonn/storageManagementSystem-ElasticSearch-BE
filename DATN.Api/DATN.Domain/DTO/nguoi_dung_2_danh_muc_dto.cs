using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DATN.Domain.DTO
{
    public class nguoi_dung_2_danh_muc_dto
    {
        public Guid? Id { get; set; }
        public Guid? nguoi_dung_id { get; set; }
        public Guid? danh_muc_id { get; set; }
    }
}
