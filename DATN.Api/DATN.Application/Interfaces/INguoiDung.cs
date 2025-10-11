using backend_v3.Dto.Common;
using DATN.Application.Utils;
using DATN.Domain.DTO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DATN.Application.Interfaces
{
    public interface INguoiDung
    {
        Task<List<nguoi_dung_dto>> GetAllNguoiDung(string? keySearch);
        Task<List<nguoi_dung_dto>> GetAllNguoiDungByPhongBan(Guid nguoi_dung_id, Guid tai_lieu_id);
        Task<List<nguoi_dung_dto>> GetDsNguoiDungInDocs(Guid tai_lieu_id); // người dùng được chia sẻ tài liệu
        Task<PaginatedList<nguoi_dung_dto>> GetPaginNguoiDung(nguoiDungPaginParams request);
        Task<nguoi_dung_dto> GetNguoiDungyId(Guid id);
        Task<List<nguoi_dung_dto>> GetNguoiDungyIds(List<Guid> ids);
        Task<nguoi_dung_dto> AddNguoiDung(nguoi_dung_dto request);
        Task<string> UpdateNguoiDung(Guid id, nguoi_dung_dto request);
        Task<string> DeleteNguoiDung(Guid id);
        Task<string> DeleteManyNguoiDung(List<Guid> ids);
        Task<List<menu_dto>> GetMenuByNguoiDung(Guid nguoi_dung_id);
        Task<List<string>> GetPermByNguoiDung(Guid nguoi_dung_id, string ma_dinh_danh);
    }
}
