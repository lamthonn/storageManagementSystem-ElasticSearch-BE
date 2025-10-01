using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.Design;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DATN.Domain.Entities
{
    public class nguoi_dung_2_danh_muc
    {
        [Key]
        public Guid Id { get; set; }
        public Guid nguoi_dung_id { get; set; }
        public Guid danh_muc_id { get; set; }
        [ForeignKey(nameof(nguoi_dung_id))]
        public virtual nguoi_dung nguoi_dung { get; set; }
        [ForeignKey(nameof(danh_muc_id))]
        public virtual danh_muc danh_muc { get; set; }
    }
}
