using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DATN.Domain.DTO
{
    public class cau_hinh_file_dto
    {
        public Guid? id { get; set; }
        public string? file_type { get; set; }
        public string? extension_file { get; set; } // đuôi file (.docx, .pdf, ...)
        public int file_size { get; set; } // Mb
    }
}
