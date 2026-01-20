using DATN.Domain.DTO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DATN.Application.Interfaces
{
    public interface IThongBaoService
    {
        Task<List<thong_bao_respone_dto>> GetThongBaosByUser(Guid userId);
        Task<string> GuiThongBao(thong_bao_request thongBaoDto);
        Task<string> HandleOkShareFile(respone_thong_bao_dto res);
        Task<string> HandleCancelShareFile(respone_thong_bao_dto thong_bao_id);
        Task<string> CapNhatTrangThai(Guid thong_bao_id);
    }
}
