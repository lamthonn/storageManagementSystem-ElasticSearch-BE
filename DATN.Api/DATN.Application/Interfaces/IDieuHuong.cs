using DATN.Domain.DTO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DATN.Application.Interfaces
{
    public interface IDieuHuong
    {
        Task<string> DongBoMenu(List<dieu_huong_dto> request);
        Task<List<dieu_huong_dto>> GetMenu();
    }
}
