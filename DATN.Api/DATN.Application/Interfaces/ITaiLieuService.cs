using DATN.Application.TaiLieu;
using DATN.Application.Utils;
using DATN.Domain.DTO;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DATN.Application.Interfaces
{
    public interface ITaiLieuService
    {
        Task<PaginatedList<tai_lieu_dto>> GetTaiLieuByPhanQuyen(tai_lieu_dto request);
        Task<IActionResult> AddTaiLieu(uploadedFileInfo request);
        Task<PaginatedList<ResultSearch>> GetDataSearch(ResultSearchParams request);
        Task<List<nguoi_dung_dto>> GetAllNguoiDungByDocs(Guid currentUserId); // lấy tất cả người dùng có tài liệu được chia sẻ với mình
        Task<DownloadResult> HandleDownloadTaiLieu(Guid idTaiLieu); // lấy tất cả người dùng có tài liệu được chia sẻ với mình
        Task<PaginatedList<ResultSearch>> GetDocsByFolder(Guid folder_id);
    }
}
