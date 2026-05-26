🇺🇸 [English Version](README.md) | 🇧🇷 Português

# Orçamento Familiar

> Aplicação web de controle de orçamento familiar — gerencie receitas, despesas fixas, faturas de cartão e saldo mensal em um só lugar.

![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet&logoColor=white)
![C#](https://img.shields.io/badge/C%23-12-239120?logo=csharp&logoColor=white)
![React](https://img.shields.io/badge/React-18-61DAFB?logo=react&logoColor=black)
![TypeScript](https://img.shields.io/badge/TypeScript-5-3178C6?logo=typescript&logoColor=white)
![Tailwind CSS](https://img.shields.io/badge/Tailwind%20CSS-3-06B6D4?logo=tailwindcss&logoColor=white)
![Docker](https://img.shields.io/badge/Docker-Compose-2496ED?logo=docker&logoColor=white)
![PostgreSQL](https://img.shields.io/badge/PostgreSQL-16-4169E1?logo=postgresql&logoColor=white)
![Licença](https://img.shields.io/badge/Licen%C3%A7a-MIT-22c55e)

O Orçamento Familiar é uma aplicação web full-stack para acompanhar as finanças da família mês a mês. Suporta múltiplos membros via sistema de convites, gerencia compras parceladas com propagação automática para meses futuros e permite importar faturas de cartão diretamente de arquivos PDF.

---

## Funcionalidades

### Dashboard Anual
- Tabela com todos os meses: Receita, Despesa Prevista, Despesa Realizada, Saldo Previsto, Saldo Real
- Gráfico de barras: Receita vs Despesa
- Gráfico de linha: evolução do Saldo ao longo do ano
- Navegação entre anos

### Visão Mensal
- **Receitas** — salários (Titular e Cônjuge) + rendas extras com CRUD completo
- **Despesas Fixas** — CRUD completo, copiadas automaticamente do mês anterior
- **Lançamentos de Fatura** — CRUD com filtros por cartão/categoria, parcelamentos automáticos com replicação nos meses seguintes, importação de fatura via PDF
- **Resumo por Categoria** — tabela + gráfico donut

### Cartões
- CRUD completo de cartões de crédito
- Acompanhamento de uso atual vs meta mensal por cartão

### Configurações e Convites
- Lista de membros ativos
- Gerenciamento de convites pendentes (copiar link ou revogar)
- Convidar novos membros por e-mail (links válidos por 7 dias, de uso único)

---

## Stack Tecnológica

| Camada | Tecnologia | Finalidade |
|--------|-----------|------------|
| API | ASP.NET Core 8 (Clean Architecture) | API REST, autenticação |
| Auth | ASP.NET Core Identity + JWT | Access token em memória + refresh token em httpOnly cookie |
| ORM | Entity Framework Core 8 | Acesso ao banco, migrations |
| Banco | PostgreSQL 16 | Armazenamento persistente |
| Frontend | React 18 + Vite + TypeScript | SPA |
| Estilo | Tailwind CSS 3 | CSS utilitário |
| Infra | Docker + Docker Compose | Orquestração de containers |

---

## Estrutura do Projeto

```
/
├── backend/
│   ├── OrcamentoFamiliar.sln
│   ├── OrcamentoFamiliar.Domain/          # Entidades, value objects
│   ├── OrcamentoFamiliar.Application/     # DTOs, interfaces, casos de uso
│   ├── OrcamentoFamiliar.Infrastructure/  # EF Core, DbContext, migrations, serviços, seeder
│   └── OrcamentoFamiliar.API/             # Controllers, Program.cs, Dockerfile
└── frontend/
    └── src/
        ├── api/         # Clientes Axios
        ├── components/  # Layout, modais (incluindo importação de PDF)
        ├── contexts/    # AuthContext
        ├── hooks/       # Custom React hooks
        ├── pages/       # Dashboard, MonthlyView, Cards, Login, Register, Settings
        ├── types/       # Tipos TypeScript
        └── utils/
```

---

## Pré-requisitos

- [Docker Desktop](https://www.docker.com/products/docker-desktop/) instalado e rodando

---

## Quick Start (Docker Compose)

```bash
git clone https://github.com/codewiththiago/orcamento-familiar.git
cd orcamento-familiar

docker-compose up --build
```

Aguarde o build (~3–5 min na primeira vez). Acesse:

| Serviço  | URL                            |
|----------|--------------------------------|
| Frontend | http://localhost               |
| API      | http://localhost:8080          |
| Swagger  | http://localhost:8080/swagger  |

### Primeiro Acesso

Na primeira execução **não há usuários**. Acesse `/register` diretamente para criar a conta inicial — nenhum convite é necessário para o primeiro usuário.

A partir daí, novos membros precisam ser convidados via **Configurações → Convidar membro**.

> O seeder cria categorias e cartões padrão automaticamente. Nenhum usuário é pré-criado.

---

## Desenvolvimento Local (sem Docker)

### Backend

Pré-requisitos: .NET 8 SDK e PostgreSQL rodando localmente.

```bash
cd backend

# Restaurar pacotes
dotnet restore OrcamentoFamiliar.sln

# Connection string padrão:
# Host=localhost;Port=5432;Database=orcamento_familiar;Username=postgres;Password=postgres
# Edite backend/OrcamentoFamiliar.API/appsettings.Development.json se necessário

# Rodar a API (migrations e seed são aplicados automaticamente na inicialização)
cd OrcamentoFamiliar.API
dotnet run
# API disponível em: http://localhost:8080
```

### Frontend

Pré-requisito: Node.js 20+

```bash
cd frontend

npm install

# Copiar o arquivo de variáveis de ambiente
cp .env.example .env.local
# VITE_API_URL=http://localhost:8080/api

npm run dev
# Disponível em: http://localhost:5173
```

---

## Variáveis de Ambiente

### Backend

| Variável | Descrição |
|----------|-----------|
| `ConnectionStrings__DefaultConnection` | Connection string do PostgreSQL |
| `Jwt__Key` | Chave secreta — string aleatória, mínimo 32 caracteres |
| `Jwt__Issuer` | `OrcamentoFamiliar` |
| `Jwt__Audience` | `OrcamentoFamiliarUsers` |
| `Frontend__Url` | URL do frontend (para CORS) |
| `ASPNETCORE_URLS` | `http://+:8080` |

### Frontend

| Variável | Descrição |
|----------|-----------|
| `VITE_API_URL` | URL da API do backend (ex: `http://localhost:8080/api`) |

---

## Sistema de Convites

Qualquer membro autenticado pode convidar outros:

1. Acesse **Configurações** no menu lateral
2. Informe o e-mail da pessoa e clique em **Convidar**
3. O link é gerado e copiado automaticamente para a área de transferência
4. Envie o link para a pessoa (WhatsApp, e-mail, etc.)
5. A pessoa abre o link → preenche nome e senha → entra no sistema

Os convites são válidos por **7 dias** e de uso único.

---

## Migrations

As migrations são aplicadas automaticamente ao iniciar o backend. Para criar uma nova migration manualmente:

```bash
cd backend/OrcamentoFamiliar.API

dotnet ef migrations add NomeDaMigration \
  --project ../OrcamentoFamiliar.Infrastructure \
  --startup-project .

dotnet ef database update \
  --project ../OrcamentoFamiliar.Infrastructure \
  --startup-project .
```

---

## Deploy

### Railway

1. Crie um projeto no [Railway](https://railway.app) e adicione um serviço PostgreSQL
2. Adicione dois serviços de Deploy apontando para este repositório

**Backend**
- Root Directory: `backend`
- Dockerfile: `OrcamentoFamiliar.API/Dockerfile`
- Variáveis de ambiente:
  ```
  ConnectionStrings__DefaultConnection=<URL do PostgreSQL>
  Jwt__Key=<chave secreta longa e aleatória>
  Jwt__Issuer=OrcamentoFamiliar
  Jwt__Audience=OrcamentoFamiliarUsers
  Frontend__Url=<URL do frontend>
  ASPNETCORE_URLS=http://+:8080
  ```

**Frontend**
- Root Directory: `frontend`
- Dockerfile: `frontend/Dockerfile`
- Variáveis de ambiente:
  ```
  VITE_API_URL=<URL do backend>/api
  ```
  > `VITE_API_URL` é injetada em build-time pelo Vite. Após definir a variável, faça um redeploy.

### Render

**Backend** — New Web Service
- Root: `backend`
- Build: `dotnet publish OrcamentoFamiliar.API/OrcamentoFamiliar.API.csproj -c Release -o out`
- Start: `dotnet out/OrcamentoFamiliar.API.dll`
- Variáveis: as mesmas do Railway acima

**Frontend** — New Static Site
- Root: `frontend`
- Build: `npm install && npm run build`
- Publish directory: `dist`
- Rewrite rule: `/* → /index.html`

---

## Roadmap

- [ ] Melhorias de layout para mobile
- [ ] Previsões de gastos anuais baseadas em histórico
- [ ] Exportação do resumo em PDF / Excel
- [ ] Suporte a múltiplas moedas
- [ ] Acompanhamento de metas de poupança

---

## Licença

MIT © [codewiththiago](https://github.com/codewiththiago)
