# Portal Urbano - Guia de Setup

Este guia explica tudo que precisa instalar para rodar o projeto localmente: dependencias, banco de dados, migracoes e inicializacao.

## 1) Pre-requisitos

- Git
- .NET SDK 10 (`10.0.x`)
- MySQL Server 8+ (ou MariaDB compativel)
- (Opcional) MySQL Workbench ou phpMyAdmin

## 2) Clonar o projeto

```bash
git clone <url-do-repo>
cd portal_urbano/portal_urbano/portal_urbano
```

## 3) Restaurar pacotes do projeto

Este projeto usa os pacotes NuGet:

- `Microsoft.EntityFrameworkCore.Tools` (9.0.0)
- `Pomelo.EntityFrameworkCore.MySql` (9.0.0)
- `supabase-csharp` (0.16.2)

Restaure com:

```bash
dotnet restore
```

## 4) Instalar ferramenta de migrations (EF CLI)

Se ainda nao tiver:

```bash
dotnet tool install --global dotnet-ef
```

Se ja tiver:

```bash
dotnet tool update --global dotnet-ef
```

## 5) Configurar `appsettings`

Arquivo: `appsettings.json`

Ajuste:

- `ConnectionStrings:DefaultConnection`
- `Supabase:Url`
- `Supabase:Key`
- `Supabase:Bucket`
- `Gemini:ApiKey`
- `Gemini:Model`

Exemplo de conexao local:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=localhost;Database=portal_urbano;User=root;Password=;"
}
```

## 6) Criar banco de dados

No MySQL/MariaDB:

```sql
CREATE DATABASE portal_urbano;
```

## 7) Criar migration inicial (estado atual do banco)

No seu projeto ja existe a pasta `Migrations/` com a migration inicial.

Em geral, para recriar do zero, voce deve:
1) garantir que o banco esta vazio (ou apagar o banco)
2) rodar `dotnet ef database update`

Se por algum motivo a migration inicial nao existir no seu clone (caso voce esteja sincronizando de um commit mais antigo),
gere novamente a primeira migration com:

```bash
dotnet ef migrations add InitialCreate
```

Isso vai criar a pasta `Migrations/` com os arquivos necessarios para recriar o schema.

## 8) Aplicar migration no banco
Importante: o `database update` falha se as tabelas ja existirem.
Entao para recriar o schema certinho, recomendo apagar o banco antes.

```bash
dotnet ef database update
```

## 9) Rodar o projeto

```bash
dotnet run
```

A aplicacao vai subir em uma URL local exibida no terminal (ex.: `https://localhost:xxxx`).

## 10) Fluxo para qualquer pessoa recriar o banco

Depois que a migration `InitialCreate` estiver versionada no Git, qualquer pessoa so precisa:

```bash
dotnet restore
dotnet ef database update
dotnet run
```

## Problemas comuns

- **Erro de conexao MySQL**: confira usuario/senha/porta na connection string.
- **Erro de permissao no banco**: conceda permissao ao usuario do MySQL.
- **`dotnet-ef` nao encontrado**: reinstale com `dotnet tool install --global dotnet-ef`.
- **Porta ocupada**: finalize o processo anterior do app e rode novamente.

## Seguranca

Nao commitar chaves reais de API no repositorio. Prefira:

- `appsettings.Development.json` local (nao versionado), ou
- User Secrets (`dotnet user-secrets`), ou
- variaveis de ambiente no servidor.

