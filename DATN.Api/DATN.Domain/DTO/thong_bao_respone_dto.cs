using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DATN.Domain.DTO
{
    public class thong_bao_respone_dto
    {
        public Guid id { get; set; }
        public string? title { get; set; }
        public string? content { get; set; }
        public DateTime? createdAt { get; set; }
        public bool? isRead { get; set; }
    }
}
