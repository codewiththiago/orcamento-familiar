# Orçamento Familiar

Aplicação web de controle de orçamento familiar para Thiago e Juh.

## Stack
- **Backend**: ASP.NET Core 8 Web API (C#) — Clean Architecture
- **Frontend**: React 18 + Vite + TypeScript + Tailwind CSS
- **Banco de dados**: PostgreSQL 16
- **ORM**: Entity Framework Core 8
- **Auth**: ASP.NET Core Identity + JWT (access token em memória + refresh token em httpOnly cookie)

---

## Como rodar com Docker (recomendado)

### Pré-requisitos
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) instalado e rodando

### Passos

```bash
# Clone ou acesse o diretório do projeto
cd orcamento-familiar

# Sobe tudo (DB + Backend + Frontend)
docker-compose up --build
```

Aguarde o build (~3-5 min na primeira vez). Depois acesse:
- **Frontend**: http://localhost
- **API**: http://localhost:8080
- **Swagger**: http://localhost:8080/swagger

### Credenciais padrão
| Usuário | Email | Senha |
|---------|-------|-------|
| Thiago | thiago@orcamento.com | Thiago@123 |
| Juh | juh@orcamento.com | Juh@123 |

---

## Como rodar localmente (desenvolvimento)

### Backend

Pré-requisito: .NET 8 SDK + PostgreSQL rodando localmente

```bash
cd backend

# Restaurar pacotes
dotnet restore OrcamentoFamiliar.sln

# Configurar connection string (edite appsettings.Development.json se necessário)
# Padrão: Host=localhost;Port=5432;Database=orcamento_familiar;Username=postgres;Password=postgres

# Criar e aplicar migrations
cd OrcamentoFamiliar.API
dotnet ef migrations add InitialCreate --project ../OrcamentoFamiliar.Infrastructure
dotnet ef database update

# Rodar a API
dotnet run
# API disponível em http://localhost:8080
```

### Frontend

Pré-requisito: Node.js 20+

```bash
cd frontend

# Instalar dependências
npm install

# Criar .env.local (copie do exemplo)
cp .env.example .env.local
# VITE_API_URL=http://localhost:8080/api

# Rodar em desenvolvimento
npm run dev
# Abre em http://localhost:5173
```

---

## Gerando Migrations (primeira vez ou após mudanças no modelo)

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

## Deploy no Railway

1. Crie um projeto no [Railway](https://railway.app)
2. Adicione um serviço PostgreSQL
3. Adicione dois serviços de Deploy (backend e frontend)

### Backend (Railway)
- **Root Directory**: `backend`
- **Dockerfile**: `OrcamentoFamiliar.API/Dockerfile`
- **Variáveis de ambiente**:
  ```
  ConnectionStrings__DefaultConnection=<URL do PostgreSQL do Railway>
  Jwt__Key=<chave secreta longa e aleatória>
  Jwt__Issuer=OrcamentoFamiliar
  Jwt__Audience=OrcamentoFamiliarUsers
  Frontend__Url=<URL do seu frontend>
  ASPNETCORE_URLS=http://+:8080
  ```

### Frontend (Railway)
- **Root Directory**: `frontend`
- **Build command**: `npm run build`
- **Start command**: Nginx (via Dockerfile)
- **Variáveis de ambiente**:
  ```
  VITE_API_URL=<URL do backend Railway>/api
  ```
  > **Importante**: Rebuild após definir `VITE_API_URL` pois é injetada em build-time pelo Vite.

### Deploy no Render

#### Backend
1. New Web Service → Connect repo
2. Root: `backend`
3. Build command: `dotnet publish OrcamentoFamiliar.API/OrcamentoFamiliar.API.csproj -c Release -o out`
4. Start command: `dotnet out/OrcamentoFamiliar.API.dll`
5. Adicione as variáveis de ambiente conforme Railway acima

#### Frontend
1. New Static Site → Connect repo
2. Root: `frontend`
3. Build command: `npm install && npm run build`
4. Publish directory: `frontend/dist`
5. Adicione rewrite rule: `/* → /index.html`

---

## Funcionalidades

### Dashboard Anual
- Tabela com todos os meses: Receita, Despesa Prevista, Despesa Realizada, Saldo Previsto, Saldo Real, Carteira XP
- Gráfico de barras: Receita vs Despesa Realizada
- Gráfico de linha: evolução do Saldo
- Navegação entre anos

### Visão Mensal (clique num mês)
- **Receitas**: salários Thiago e Juh + rendas extras (CRUD)
- **Despesas Fixas**: tabela com CRUD completo (copiadas automaticamente do mês anterior)
- **Lançamentos de Fatura**: CRUD com filtros por cartão/categoria, parcelamentos automáticos
- **Resumo por Categoria**: tabela + gráfico donut
- **Carteira XP**: planejado vs realizado

### Cartões
- CRUD de cartões
- Visualização de uso atual vs meta do mês

---

## Estrutura do projeto

```
/
├── backend/
│   ├── OrcamentoFamiliar.sln
│   ├── OrcamentoFamiliar.Domain/          # Entidades puras
│   ├── OrcamentoFamiliar.Application/     # DTOs, Interfaces, Services
│   ├── OrcamentoFamiliar.Infrastructure/  # EF Core, DbContext, Seeder
│   └── OrcamentoFamiliar.API/             # Controllers, Program.cs
├── frontend/
│   └── src/
│       ├── api/         # Axios clients
│       ├── components/  # Layout, charts, modals
│       ├── contexts/    # AuthContext
│       ├── pages/       # Dashboard, MonthlyView, Cards, Login
│       └── types/       # TypeScript types
├── docker-compose.yml
└── README.md
```
