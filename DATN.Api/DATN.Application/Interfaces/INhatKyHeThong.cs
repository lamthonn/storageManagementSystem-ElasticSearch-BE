using DATN.Application.Utils;
using DATN.Domain.DTO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DATN.Application.Interfaces
{
    public interface INhatKyHeThong
    {
        Task<PaginatedList<nhat_ky_he_thong_dto>> GetAllLog(nhat_ky_he_thong_pagin_param request);
        Task<nhat_ky_he_thong_dto> AddLog(nhat_ky_he_thong_dto request);
    }
}
