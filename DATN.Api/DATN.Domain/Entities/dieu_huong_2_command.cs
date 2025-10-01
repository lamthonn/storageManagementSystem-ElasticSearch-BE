using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DATN.Domain.Entities
{
    public class dieu_huong_2_command
    {
        public Guid Id { get; set; }
        public Guid dieu_huong_id { get; set; }
        [StringLength(512)]
        public string command { get; set; }
        public Guid? command_id { get; set; }
        [ForeignKey(nameof(dieu_huong_id))]
        public virtual dieu_huong dieu_Huong { get; set; }
        [ForeignKey(nameof(command_id))]
        public virtual dm_command dm_command { get; set; }
    }
}
