using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DATN.Domain.DTO
{
    public class command_dto
    {
        public Guid? id { get; set; }
        public string? command { get; set; }
        public string? ten { get; set; }
    }
}
