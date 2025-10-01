using DATN.Application.Utils;
using DATN.Domain.DTO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DATN.Application.Interfaces
{
    public interface IDanhMucCapDo
    {
        Task<PaginatedList<danh_muc_dto>> GetAll(danh_muc_dto request);
        Task<danh_muc_dto> GetById(Guid id);
        Task<danh_muc_dto> Create(danh_muc_dto obj);
        Task<danh_muc_dto> Update(danh_muc_dto obj);
        Task<bool> Delete(Guid id);
        Task<bool> DeleteAny(List<Guid> ids);
    }
}
