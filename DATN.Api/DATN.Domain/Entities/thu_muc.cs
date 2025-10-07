using DATN.Domain.Entities.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DATN.Domain.Entities
{
    public class thu_muc : BaseAuditableEntity
    {
        [Key]
        public Guid id { get; set; }
        public string ten { get; set; }
        public Guid? thu_muc_cha_id { get; set; }
        public Guid nguoi_dung_id { get; set; }
        [ForeignKey(nameof(nguoi_dung_id))]
        public virtual nguoi_dung Nguoi_dung { get; set; }
        public virtual ICollection<tai_lieu> ds_tai_lieu { get; set; }
    }
}
