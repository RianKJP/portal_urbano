using System.ComponentModel.DataAnnotations;

namespace portal_urbano.Models
{
    public class Categoria
    {
        [Key]
        public int IdCategoria { get; set; }

        public string Nome { get; set; }
        public string Descricao { get; set; }
        public string? Icone { get; set; }

        public List<Denuncia> Denuncias { get; set; }
    }
}