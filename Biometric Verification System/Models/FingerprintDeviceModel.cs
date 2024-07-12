using Biometric_Verification_System.Areas.Identity.Data;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Biometric_Verification_System.Models
{
    public class FingerprintDeviceModel
    {

        [Key]
        public int FingerprintId { get; set; }
        public byte[] Data { get; set; }

        public DateTime Timestamp { get; set; } = DateTime.Now;

        public string? Id { get; set; }
        [ForeignKey("Id")]
        public virtual BioVerifyUser? GetBioUser { get; set; }

    }
}
