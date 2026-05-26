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

        public int Avisos { get; set; } = 0;
        public bool Banido { get; set; } = false;

        public DateTime? CriadoEm { get; set; }

        public List<Denuncia> Denuncias { get; set; } = new List<Denuncia>();
    }
}
