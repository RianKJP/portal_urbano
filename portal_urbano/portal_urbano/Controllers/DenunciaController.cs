namespace portal_urbano.Controllers
{
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.EntityFrameworkCore;
    using portal_urbano.Data;
    using portal_urbano.Models;
    using System.Globalization;
    using System.Text;
    using System.Text.RegularExpressions;

    [ApiController]
    [Route("api/[controller]")]
    public class DenunciaController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly Supabase.Client _supabaseClient;
        private readonly IConfiguration _configuration;
        private const int MaxAvisosParaBanimento = 3;
        private const int TotalReportesParaListaNegra = 5;
        private const int TotalReportesParaRemocao = 10;
        private const string StatusListaNegra = "Lista Negra";
        private const string StatusRemovida = "Removida";

        private static readonly string[] TermosOfensivos =
        [
            "idiota",
            "imbecil",
            "otario",
            "otaria",
            "burro",
            "burra",
            "bosta",
            "merda",
            "porra",
            "caralho",
            "fdp",
            "foda-se",
            "fodase",
            "vtnc",
            "vai tomar no cu",
            "arrombado",
            "desgracado",
            "desgracada",
            "corno",
            "puta",
            "puto",
            "vagabundo",
            "vagabunda",
            "lixo",
            "babaca",
            "trouxa",
            "canalha",
            "safado",
            "safada",
            "filho da puta",
            "imbecis",
            "retardado",
            "retardada"
        ];

        private static readonly string[] TermosPoluicao =
        [
            "spam",
            "cassino",
            "apostas",
            "pornografia",
            "porno",
            "xxx",
            "ganhe dinheiro",
            "renda extra",
            "clique aqui",
            "compre agora",
            "http://",
            "https://",
            "www.",
            "bit.ly",
            "tigrinho",
            "fortune tiger",
            "1xbet",
            "blaze",
            "betano",
            "ganho facil",
            "dinheiro facil",
            "sorteio",
            "gratis",
            "promocao",
            "desconto",
            "comprar seguidores",
            "pix gratis",
            "urubu do pix",
            "renda passiva"
        ];

        private static readonly string[] TermosViolenciaAmeaca =
        [
            "matar",
            "morrer",
            "assassinar",
            "agredir",
            "linchar",
            "bater",
            "surra",
            "espancar",
            "tiro",
            "arma",
            "facada",
            "terrorista",
            "atentado",
            "morte"
        ];

        public DenunciaController(AppDbContext context, Supabase.Client supabaseClient, IConfiguration configuration)
        {
            _context = context;
            _supabaseClient = supabaseClient;
            _configuration = configuration;
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var usuarioIdAtual = 1; // MOCK: Pegar do usuário logado no futuro

            var denunciasBase = await _context.Denuncias
                .AsNoTracking()
                .AsSplitQuery()
                .Include(d => d.Usuario)
                .Include(d => d.Categoria)
                .Include(d => d.Likes)
                .Include(d => d.Reportes)
                .Include(d => d.Comentarios)
                    .ThenInclude(c => c.Usuario)
                .Where(d => d.Status != StatusRemovida)
                .ToListAsync();

            var denuncias = denunciasBase
                .OrderByDescending(d => d.Likes.Count + d.Comentarios.Count)
                .ThenByDescending(d => d.Comentarios.Count)
                .ThenByDescending(d => d.Likes.Count)
                .ThenByDescending(d => d.CriadoEm)
                .Select(d => new {
                    d.IdDenuncia,
                    d.Titulo,
                    d.Descricao,
                    d.Status,
                    Categoria = d.Categoria != null ? d.Categoria.Nome : "Geral",
                    d.Latitude,
                    d.Longitude,
                    Endereco = $"{d.Rua}, {d.Bairro} - {d.Cidade}/{d.Uf}", 
                    d.CriadoEm,
                    d.ImagemUrl,
                    NomeUsuario = d.StatusAnonimo == 1 ? "Usuário Anônimo" : (d.Usuario != null ? d.Usuario.Nome : "Desconhecido"),
                    TotalLikes = d.Likes.Count,
                    TotalComentarios = d.Comentarios.Count,
                    TotalInteracoes = d.Likes.Count + d.Comentarios.Count,
                    TotalReportes = d.Reportes.Select(r => r.IdUsuario).Distinct().Count(),
                    LikedByUser = d.Likes.Any(l => l.UsuarioId == usuarioIdAtual),
                    Comentarios = d.Comentarios.OrderBy(c => c.CriadoEm).Select(c => new {
                        c.IdComentario,
                        c.Texto,
                        c.CriadoEm,
                        NomeUsuario = c.Usuario != null ? c.Usuario.Nome : "Anônimo",
                        IsOwner = c.IdUsuario == usuarioIdAtual // se pode excluir
                    }).ToList()
                })
                .ToList();

            return Ok(denuncias);
        }

        [HttpPost]
        public async Task<IActionResult> Post(
            [FromForm] portal_urbano.Models.Denuncia novaDenuncia, 
            [FromForm] IFormFile[] imagens,
            [FromForm] string? anonimo,
            [FromForm] string? lat_str,
            [FromForm] string? lon_str)
        {
            try
            {
                var usuario = await ObterUsuarioAtualAsync(criarSeNaoExistir: true);
                if (usuario == null)
                {
                    return Unauthorized();
                }

                if (usuario.Banido)
                {
                    return RedirecionarParaAvisoModeracao(usuario);
                }

                if (ConteudoViolaModeracao(novaDenuncia.Titulo, novaDenuncia.Descricao))
                {
                    usuario.Avisos++;

                    if (usuario.Avisos >= MaxAvisosParaBanimento)
                    {
                        usuario.Banido = true;
                    }

                    await _context.SaveChangesAsync();
                    return RedirecionarParaAvisoModeracao(usuario);
                }

                // Limpa o que o model binder possa ter tentado fazer errado
                novaDenuncia.Latitude = 0;
                novaDenuncia.Longitude = 0;

                // Tenta converter coordenadas manualmente usando InvariantCulture (força o ponto como separador)
                if (!string.IsNullOrEmpty(lat_str))
                {
                    decimal.TryParse(lat_str.Replace(",", "."), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal lat);
                    novaDenuncia.Latitude = lat;
                }
                if (!string.IsNullOrEmpty(lon_str))
                {
                    decimal.TryParse(lon_str.Replace(",", "."), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal lon);
                    novaDenuncia.Longitude = lon;
                }

                // Dados básicos
                novaDenuncia.CriadoEm = DateTime.UtcNow;
                novaDenuncia.Status = "Aberta";
                
                // Tratar usuário anônimo
                novaDenuncia.StatusAnonimo = (anonimo == "on") ? 1 : 0;

                // Associa a denúncia ao usuário logado (MOCK: pegando o primeiro usuário do banco, ou ID = 1)
                novaDenuncia.IdUsuario = usuario.IdUsuario;

                // Garantir que a categoria enviada pelo formulário exista no banco
                if (novaDenuncia.IdCategoria == 0) novaDenuncia.IdCategoria = 1;

                // 1. Salva no SQL primeiro para gerar o ID
                _context.Denuncias.Add(novaDenuncia);
                await _context.SaveChangesAsync();

                // 2. Fazer upload para o Supabase se houver imagens
                if (imagens != null && imagens.Length > 0)
                {
                    var urls = new List<string>();
                    var bucketName = _configuration["Supabase:Bucket"] ?? "imagens_denuncias";

                    for (int i = 0; i < imagens.Length; i++)
                    {
                        var imagem = imagens[i];
                        if (imagem.Length > 0)
                        {
                            using var memoryStream = new MemoryStream();
                            await imagem.CopyToAsync(memoryStream);
                            var bytes = memoryStream.ToArray();

                            // Renomear: denuncia_{Id}_{Indice}.{extensão}
                            var extensao = Path.GetExtension(imagem.FileName);
                            var nomeArquivo = $"denuncia_{novaDenuncia.IdDenuncia}_{i}{extensao}";

                            // Upload para o Supabase
                            await _supabaseClient.Storage
                                .From(bucketName)
                                .Upload(bytes, nomeArquivo, new Supabase.Storage.FileOptions { CacheControl = "3600", Upsert = true });

                            // Pega a URL pública gerada
                            var urlPublica = _supabaseClient.Storage.From(bucketName).GetPublicUrl(nomeArquivo);
                            urls.Add(urlPublica);
                        }
                    }

                    // 3. Atualiza a tabela com a URL (separado por vírgula se for mais de uma)
                    novaDenuncia.ImagemUrl = string.Join(",", urls);
                    await _context.SaveChangesAsync();
                }

                // Redireciona de volta pro Feed ou retorna sucesso
                return Redirect("/Home/Feed");
            }
            catch (Exception ex)
            {
                return BadRequest($"Erro ao salvar a denúncia: {ex.Message}");
            }
        }

        [HttpPost("{id}/reportar")]
        public async Task<IActionResult> ReportarDenuncia(int id)
        {
            var usuario = await ObterUsuarioAtualAsync();
            if (usuario == null) return Unauthorized();
            if (usuario.Banido) return StatusCode(StatusCodes.Status403Forbidden, "Usuario banido do sistema.");

            var denuncia = await _context.Denuncias
                .Include(d => d.Reportes)
                .FirstOrDefaultAsync(d => d.IdDenuncia == id);

            if (denuncia == null) return NotFound("Denuncia nao encontrada.");
            if (denuncia.Status == StatusRemovida) return BadRequest("Esta denuncia ja foi removida.");

            var usuarioJaReportou = await _context.Reportes
                .AnyAsync(r => r.IdDenuncia == id && r.IdUsuario == usuario.IdUsuario);

            if (usuarioJaReportou)
            {
                var totalAtual = await ContarReportesUnicosAsync(id);
                return Conflict(new
                {
                    message = "Voce ja denunciou esta denuncia.",
                    totalReportes = totalAtual,
                    status = denuncia.Status
                });
            }

            _context.Reportes.Add(new Reporte
            {
                IdDenuncia = id,
                IdUsuario = usuario.IdUsuario,
                Motivo = "Denuncia reportada pelo feed",
                CriadoEm = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            var totalReportes = await ContarReportesUnicosAsync(id);

            if (totalReportes >= TotalReportesParaRemocao)
            {
                denuncia.Status = StatusRemovida;
            }
            else if (totalReportes >= TotalReportesParaListaNegra)
            {
                denuncia.Status = StatusListaNegra;
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = denuncia.Status == StatusRemovida
                    ? "Denuncia removida automaticamente apos 10 reportes."
                    : "Denuncia registrada com sucesso.",
                totalReportes,
                status = denuncia.Status,
                removida = denuncia.Status == StatusRemovida
            });
        }

        [HttpPost("{id}/like")]
        public async Task<IActionResult> ToggleLike(int id)
        {
            // Pega o usuário logado (mock para "Usuário Anônimo" ou id 1)
            var usuario = await _context.Usuarios.FirstOrDefaultAsync();
            if (usuario == null) return Unauthorized();
            if (usuario.Banido) return StatusCode(StatusCodes.Status403Forbidden, "Usuario banido do sistema.");

            var like = await _context.Gostei.FirstOrDefaultAsync(g => g.DenunciaId == id && g.UsuarioId == usuario.IdUsuario);

            if (like != null)
            {
                // Já curtiu, então exclui (unlike)
                _context.Gostei.Remove(like);
                await _context.SaveChangesAsync();
                return Ok(new { liked = false });
            }
            else
            {
                // Adiciona o like
                _context.Gostei.Add(new portal_urbano.Models.Gostei { DenunciaId = id, UsuarioId = usuario.IdUsuario });
                await _context.SaveChangesAsync();
                return Ok(new { liked = true });
            }
        }

        [HttpPost("{id}/comentario")]
        public async Task<IActionResult> AddComentario(int id, [FromForm] string texto)
        {
            if (string.IsNullOrWhiteSpace(texto)) return BadRequest("O comentário não pode ser vazio.");

            var usuario = await _context.Usuarios.FirstOrDefaultAsync();
            if (usuario == null) return Unauthorized();
            if (usuario.Banido) return StatusCode(StatusCodes.Status403Forbidden, "Usuario banido do sistema.");

            var comentario = new portal_urbano.Models.Comentario
            {
                IdDenuncia = id,
                IdUsuario = usuario.IdUsuario,
                Texto = texto,
                CriadoEm = DateTime.UtcNow
            };

            _context.Comentarios.Add(comentario);
            await _context.SaveChangesAsync();

            return Ok(new { 
                id = comentario.IdComentario, 
                texto = comentario.Texto, 
                usuario = usuario.Nome,
                criadoEm = comentario.CriadoEm
            });
        }

        [HttpDelete("comentario/{idComentario}")]
        public async Task<IActionResult> DeleteComentario(int idComentario)
        {
            var comentario = await _context.Comentarios.FindAsync(idComentario);
            if (comentario == null) return NotFound();

            // Num sistema real, verificar se o usuário logado é o dono do comentário!
            _context.Comentarios.Remove(comentario);
            await _context.SaveChangesAsync();

            return Ok();
        }

        private Task<int> ContarReportesUnicosAsync(int idDenuncia)
        {
            return _context.Reportes
                .Where(r => r.IdDenuncia == idDenuncia)
                .Select(r => r.IdUsuario)
                .Distinct()
                .CountAsync();
        }

        private async Task<Usuario?> ObterUsuarioAtualAsync(bool criarSeNaoExistir = false)
        {
            var usuario = await _context.Usuarios.FirstOrDefaultAsync();

            if (usuario == null && criarSeNaoExistir)
            {
                usuario = new Usuario
                {
                    Nome = "Usuario Real",
                    Email = "real@portal.com",
                    SenhaHash = "123",
                    Avisos = 0,
                    Banido = false,
                    CriadoEm = DateTime.UtcNow
                };

                _context.Usuarios.Add(usuario);
                await _context.SaveChangesAsync();
            }

            return usuario;
        }

        private static bool ConteudoViolaModeracao(string? titulo, string? descricao)
        {
            var conteudo = NormalizarTexto($"{titulo} {descricao}");

            if (string.IsNullOrWhiteSpace(conteudo))
            {
                return false;
            }

            var possuiOfensa = TermosOfensivos.Any(termo => ContemTermoInteiro(conteudo, termo));
            var possuiPoluicao = TermosPoluicao.Any(termo => conteudo.Contains(NormalizarTexto(termo), StringComparison.Ordinal));
            var possuiViolencia = TermosViolenciaAmeaca.Any(termo => ContemTermoInteiro(conteudo, termo));

            return possuiOfensa || possuiPoluicao || possuiViolencia;
        }

        private static bool ContemTermoInteiro(string conteudoNormalizado, string termo)
        {
            var termoNormalizado = Regex.Escape(NormalizarTexto(termo)).Replace("\\ ", "\\s+");
            var padrao = $@"(^|[^a-z0-9]){termoNormalizado}([^a-z0-9]|$)";

            return Regex.IsMatch(conteudoNormalizado, padrao, RegexOptions.CultureInvariant);
        }

        private static string NormalizarTexto(string? texto)
        {
            if (string.IsNullOrWhiteSpace(texto))
            {
                return string.Empty;
            }

            var textoNormalizado = texto.ToLowerInvariant().Normalize(NormalizationForm.FormD);
            var resultado = new StringBuilder(textoNormalizado.Length);

            foreach (var caractere in textoNormalizado)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(caractere) == UnicodeCategory.NonSpacingMark)
                {
                    continue;
                }

                var caractereNormalizado = caractere switch
                {
                    '@' => 'a',
                    '4' => 'a',
                    '0' => 'o',
                    '1' => 'i',
                    '3' => 'e',
                    '$' => 's',
                    _ => caractere
                };

                resultado.Append(caractereNormalizado);
            }

            return resultado.ToString().Normalize(NormalizationForm.FormC);
        }

        private RedirectResult RedirecionarParaAvisoModeracao(Usuario usuario)
        {
            var status = usuario.Banido ? "banido" : "aviso";
            return Redirect($"/Home/CriarDenuncia?moderacao={status}&avisos={usuario.Avisos}");
        }

        [HttpGet("categorias")]
        public async Task<IActionResult> GetCategorias()
        {
            var categorias = await _context.Categorias
                .Select(c => new {
                    c.IdCategoria,
                    c.Nome,
                    c.Descricao,
                    c.Icone
                })
                .ToListAsync();

            return Ok(categorias);
        }
    }
}
