using System.ComponentModel.DataAnnotations;

namespace frontend.Models
{
    public class BinaryFile
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public string Name { get; set; }

        [Required]
        public byte[] Data { get; set; }
    }
}