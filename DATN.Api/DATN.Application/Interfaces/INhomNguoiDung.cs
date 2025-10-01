using DATN.Application.Utils;
using DATN.Domain.DTO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DATN.Application.Interfaces
{
    public interface INhomNguoiDung
    {
        Task<nhom_nguoi_dung_dto> GetNhomNguoiDung(Guid nguoi_dung_id);
        Task<nhom_nguoi_dung_dto> GetNhomNguoiDungById(Guid id);
        Task<PaginatedList<nhom_nguoi_dung_dto>> GetPaginNhomNguoiDung(nhom_nguoi_dung_dto request); 
        Task<List<dieu_huong_dto>> GetDsMenuByNND(Guid nguoi_dung_id);
        Task<string> PhanQuyen(PhanQuyenDto request);
        Task<List<dieu_huong_dto>> GetPhanQuyen(Guid nhom_nguoi_dung_id);
        Task<string> AddNhomnguoiDung(nhom_nguoi_dung_dto request);
        Task<string> EditNhomnguoiDung(Guid id, nhom_nguoi_dung_dto request);
        Task<string> DeleteNhomnguoiDung(Guid id);
        Task<string> DeleteManyNhomnguoiDung(List<Guid> ids);

    }

    public class PhanQuyenDto
    {
        public Guid nhom_Nguoi_dung_id { get; set; }
        public List<dieu_huong_dto> ds_dieu_huong { get; set; }
    }


}
