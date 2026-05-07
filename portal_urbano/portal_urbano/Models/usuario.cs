using System.ComponentModel.DataAnnotations;

namespace portal_urbano.Models
{
    public class Usuario
    {
        [Key]
        public int IdUsuario { get; set; }

        public string Nome { get; set; }
        public string Email { get; set; }
        public string SenhaHash { get; set; }
        public string? Telefone { get; set; }

        public DateTime CriadoEm { get; set; } = DateTime.Now;

        public List<Denuncia> Denuncias { get; set; }
    }
}