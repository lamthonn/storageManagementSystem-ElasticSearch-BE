using DATN.Domain.DTO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DATN.Application.Interfaces
{
    public interface IThuMucService
    {
        Task<List<thu_muc_dto>> GetAll(Guid request);
        Task<thu_muc_dto> AddThuMuc(thu_muc_dto request);
        Task<thu_muc_dto> UpdateThuMuc(Guid id, thu_muc_dto request);
        Task<string> DeleteThuMuc(Guid id);

    }
}
