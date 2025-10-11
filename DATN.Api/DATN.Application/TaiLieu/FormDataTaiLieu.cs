using backend_v3.Dto.Common;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DATN.Application.TaiLieu
{
    public class uploadedFileInfo
    {
        public List<IFormFile> files { get; set; }
        public Guid? cap_do_id { get; set; }
        public Guid? thu_muc_id { get; set; } // id của folder đang lưu trữ5420
        public Guid? phong_ban_id { get; set; } //người dùng có thể có nhiều phòng ban => người dùng chọn phòng ban mà mình muốn lưu tài liệu
    }

    public class ResultSearch // kết quả tìm kiếm
    {
        public Guid? id { get; set; }
        public string? ten { get; set; }
        public string? ten_chu_so_huu { get; set; }
        public DateTime? ngay_tao { get; set; }
        public DateTime? ngay_sua_doi { get; set; }
        public bool? is_folder { get; set; } // true: thư mục || false: tệp
        public long? kich_co_tep { get; set; }
        public string? extension { get; set; } // extension
        public int? loai_tai_lieu { get; set; } // 1:tài liệu (word) || 2: Bảng tính (Excel) || 3: PDF || 4: Hình ảnh
        public string? plain_text { get; set; } // đường dẫn file
        public Guid? thu_muc_id { get; set; }
    }

    public class ResultSearchParams : PaginParams  // kết quả tìm kiếm
    {
        public Guid current_user_id { get; set; } // id người dùng hiện tại
        public string? keySearch { get; set; } // search theo tên thư mục hoặc tên file (PHỤC VỤ TÌM KIẾM NHANH)

        public int? loai_tai_lieu { get; set; } // 1:tài liệu (word) || 2: Bảng tính (Excel) || 3: PDF || 4: Hình ảnh
        public int? trang_thai { get; set; }  // 1: Tài liệu của tôi || 2: Tài liệu được chia sẻ với tôi 
        public Guid? nguoi_dung_id { get; set; } // lọc tài liệu theo người dùng (ví dụ có 2 tài khoản khác share tài liệu cho mình thì chỉ được select theo các tài khoản dã share TL cho mình)
        public string? keyWord { get; set; } // từ khóa có trong văn bản
        public string? ten_muc { get; set; } // search theo tên thư mục hoặc tên file
        public DateTime? ngay_tao_from { get; set; } 
        public DateTime? ngay_tao_to { get; set; } 
        public DateTime? ngay_chinh_sua_from { get; set; } 
        public DateTime? ngay_chinh_sua_to { get; set; }
        public Guid? thu_muc_id { get; set; }

    }
    public class ShareFileParams
    {
        public Guid tai_lieu_id { get; set; }
        public List<Guid> ds_nguoi_dung { get; set; }

    }

    public class ChangenameParams
    {
        public Guid tai_lieu_id { get; set; }
        public string new_name { get; set; }

    }
}
