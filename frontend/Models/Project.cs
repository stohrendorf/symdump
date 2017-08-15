using System.ComponentModel.DataAnnotations;

namespace frontend.Models
{
    public class Project
    {
        [Key]
        public int Id { get; set; }
        
        public BinaryFile Sym { get; set; }
        public BinaryFile Exe { get; set; }
    }
}