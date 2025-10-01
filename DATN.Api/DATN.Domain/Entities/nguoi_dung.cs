using DATN.Domain.Entities.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DATN.Domain.Entities
{
    public class nguoi_dung : BaseAuditableEntity
    {
        public nguoi_dung()
       : base()
        {
            this.ds_nhom_nguoi_dung = new HashSet<nguoi_dung_2_nhom_nguoi_dung>();
        }
        public Guid Id { get; set; }
        [StringLength(255)]
        public string tai_khoan { get; set; }
        [StringLength(255)]
        public string mat_khau { get; set; }
        public string salt_code { get; set; }
        public string ten { get; set; }
        public DateTime? ngay_sinh { get; set; }
        public string? email { get; set; }
        [StringLength(32)]
        public string? so_dien_thoai { get; set; }
        public bool? gioi_tinh { get; set; }
        public bool? is_doi_mk { get; set; }
        public bool? trang_thai { get; set; } // true: Hoạt động, false: Không hoạt động
        public DateTime? ngay_doi_mk { get; set; }
        public string? RefreshToken { get; set; } // Lưu Refresh Token
        public DateTime? RefreshTokenExpiryTime { get; set; } // Hạn sử dụng của Refresh Token
        public virtual ICollection<nguoi_dung_2_nhom_nguoi_dung> ds_nhom_nguoi_dung { get; set; }
        public virtual ICollection<nguoi_dung_2_danh_muc> ds_phong_ban { get; set; }
        public virtual ICollection<tai_lieu_2_nguoi_dung> ds_tai_lieu { get; set; }


    }
}
