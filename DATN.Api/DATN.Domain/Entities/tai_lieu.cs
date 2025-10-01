using DATN.Domain.Entities.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DATN.Domain.Entities
{
    public class tai_lieu : BaseAuditableEntity
    {
        [Key]
        public Guid Id { get; set; }
        public string ma { get; set; }
        public string ten { get; set; }
        public string duong_dan { get; set; }
        public int cap_do { get; set; }
        public string phong_ban { get; set; }
        public bool is_share { get; set; }
        public int phien_ban { get; set; }
        public virtual ICollection<tai_lieu_2_nguoi_dung> ds_nguoi_dung { get; set; }

        #region Cập nhật 30/09/2025
        public bool isPublic { get; set; } // thể hiện tài liệu là public (ai cũng có thể nhìn thấy) || private (chỉ những người được chia sẻ mới có thể nhìn thấy)
        public string? FileType { get; set; } //(mime type: pdf, docx, jpg…).
        public long FileSize { get; set; } //(dung lượng).
        public string? FileHash { get; set; } //(MD5/SHA256) → để kiểm tra trùng file.
        public string? Status { get; set; } // trạng thái: uploaded, indexed...

        // Phục vụ ElasticSearch
        public string? ContentText { get; set; }    // nội dung text trích xuất từ file
        public DateTime? IndexedAt { get; set; }  // (ngày giờ đã index lên Elastic).
        public string? IndexStatus { get; set; } // (chưa index / thành công / lỗi).
        #endregion


    }
}
