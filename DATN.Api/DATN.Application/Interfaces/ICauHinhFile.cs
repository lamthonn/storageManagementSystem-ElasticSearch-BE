using DATN.Domain.DTO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DATN.Application.Interfaces
{
    public interface ICauHinhFile
    {
        public Task<List<cau_hinh_file_dto>> getAllConfig();
        public Task<string> editConfig(List<cau_hinh_file_dto> cau_Hinh_File_Dto);

    }
}
