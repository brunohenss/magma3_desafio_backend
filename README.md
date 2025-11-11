# Magma3 Backend Assessment

Solução completa do desafio técnico para a posição de Pleno Backend Developer na Magma3.

## Sumário

Projeto desenvolvido em .NET 8 implementando Clean Architecture com todas as cinco questões propostas no desafio técnico. A solução demonstra expertise em consumo de APIs externas, desenvolvimento de APIs RESTful, integração com MongoDB, aplicação de design patterns e containerização com Docker.

## Tecnologias Utilizadas

### Core Stack
- .NET 8.0
- C# 12
- ASP.NET Core Web API
- MongoDB 7.0

### Bibliotecas e Frameworks
- **Polly 8.2.0** - Resilience patterns (Retry e Circuit Breaker)
- **Serilog 3.1.1** - Logging estruturado
- **MongoDB.Driver 2.23.1** - Acesso ao MongoDB
- **Swashbuckle 6.6.2** - Documentação OpenAPI/Swagger

### Integrações Externas (Questão 5)
- **DocuSign.eSign 3.0.0-rc** - Assinatura eletrônica de documentos
- **Microsoft.Graph 5.96.0** - Integração com Azure AD e Microsoft 365
- **Azure.Identity 1.17.0** - Autenticação Microsoft
- **GoogleMaps.LocationServices 1.2.0.5** - Geocoding e mapas

### DevOps
- Docker e Docker Compose
- Multi-stage builds
- Health checks

## Arquitetura

O projeto segue os princípios de Clean Architecture com separação clara de responsabilidades:

```
MagmaBackendAssessment/
├── src/
│   ├── MagmaAssessment.API/              # Presentation Layer
│   │   ├── Controllers/                   # REST Controllers
│   │   ├── Program.cs                     # DI e pipeline HTTP
│   │   └── appsettings.json              # Configurações
│   ├── MagmaAssessment.Core/             # Domain Layer
│   │   ├── Models/                        # Entidades de negócio
│   │   └── Interfaces/                    # Contratos de serviço
│   ├── MagmaAssessment.Infrastructure/   # Infrastructure Layer
│   │   ├── Repositories/                  # Implementação de repositórios
│   │   └── Services/                      # Serviços externos
│   └── MagmaAssessment.Tests/            # Test Layer
├── docker-compose.yml
├── Dockerfile
└── README.md
```

### Padrões de Projeto Implementados

**Repository Pattern**: Abstração do acesso a dados para Produtos (in-memory) e Clientes (MongoDB).

**Dependency Injection**: Configuração via ASP.NET Core DI Container para todos os serviços e repositórios.

**Service Layer Pattern**: Separação da lógica de integração com APIs externas (Force1, Google Maps, DocuSign, Microsoft Graph).

**Retry Pattern**: Implementado com Polly para tentativas automáticas em caso de falhas temporárias (3 tentativas com exponential backoff).

**Circuit Breaker Pattern**: Proteção contra falhas em cascata ao consumir a API Force1 (abre após 3 falhas consecutivas).

**Singleton Pattern**: Repositório de produtos mantém estado em memória durante a execução da aplicação.

**Thread-Safe Operations**: Uso de ConcurrentDictionary no repositório de produtos para operações concorrentes seguras.

## Questões Implementadas

### Questão 1: Consumo da API Force1

**Implementação**: `Force1Service.cs`

Consome a API Force1 da Magma3 para obter ativos computacionais e identifica computadores inativos (mais de 60 dias sem comunicação).

**Recursos implementados**:
- Polly com Retry Policy (3 tentativas, exponential backoff)
- Circuit Breaker para proteção contra falhas em cascata
- Fallback para dados mock quando API indisponível
- Logging detalhado de todas operações
- Tratamento robusto de códigos HTTP (401, 403, 404, 500, etc)

**Endpoints**:
```
GET /ativos                    - Lista todos os ativos
GET /computadores-inativos     - Computadores >60 dias sem comunicação
GET /ativo/{id}                - Busca ativo por ID
```

### Questão 2: API REST de Produtos

**Implementação**: `ProdutosController.cs` + `ProdutoRepository.cs`

API RESTful completa para gerenciamento de produtos com operações CRUD.

**Recursos implementados**:
- Repository Pattern com implementação in-memory (ConcurrentDictionary)
- Thread-safe operations para ambiente concorrente
- Data Annotations para validação de entrada
- Seed data com 8 produtos para demonstração
- Status codes HTTP apropriados (200, 201, 400, 404, 500)
- Singleton lifetime para manter estado entre requisições

**Endpoints**:
```
GET    /api/produtos          - Lista todos os produtos
GET    /api/produtos/{id}     - Busca produto por ID
POST   /api/produtos          - Cria novo produto
PUT    /api/produtos/{id}     - Atualiza produto
DELETE /api/produtos/{id}     - Remove produto
GET    /api/produtos/ativos   - Lista produtos ativos
```

### Questão 3: Integração com MongoDB

**Implementação**: `ClienteRepository.cs` + `ClientesController.cs`

Sistema completo de gerenciamento de clientes com persistência em MongoDB.

**Recursos implementados**:
- MongoDB Driver com operações assíncronas
- Índices automáticos (unique index em email, index em nome)
- Validação de email duplicado com exceções apropriadas
- Connection string mascarada nos logs para segurança
- CRUD completo e assíncrono
- Tratamento robusto de erros de conexão

**Endpoints**:
```
GET    /api/clientes                         - Lista todos os clientes
GET    /api/clientes/{id}                    - Busca por ID
GET    /api/clientes/email/{email}           - Busca por email
POST   /api/clientes                         - Cria cliente
PUT    /api/clientes/{id}                    - Atualiza cliente
DELETE /api/clientes/{id}                    - Remove cliente
GET    /api/clientes/verificar-email/{email} - Verifica existência
```

### Questão 4: Correção de Código

**Implementação**: `Questao4_CorrecaoCodigo.cs`

Código original corrigido com documentação detalhada de cada erro identificado.

**Erros corrigidos**:
1. `ReadAsString()` → `ReadAsStringAsync()` com await
2. `Ativo` → `List<Ativo>` (retorno correto da API)
3. Newtonsoft.Json → System.Text.Json (biblioteca nativa)
4. Adicionado `EnsureSuccessStatusCode()` para validação HTTP
5. Configurado `PropertyNameCaseInsensitive` para deserialização

### Questão 5: Integrações com SDKs

#### Google Maps API (Geocoding)
**Implementação**: `GoogleMapsService.cs`

**Métodos implementados**:
- `ObterCoordenadasAsync()` - Geocoding: converte endereço em coordenadas
- `ObterEnderecoPorCoordenadasAsync()` - Reverse Geocoding: converte coordenadas em endereço
- `CalcularDistanciaKm()` - Calcula distância entre coordenadas usando fórmula de Haversine

#### DocuSign eSignature API
**Implementação**: `DocuSignService.cs`

**Métodos implementados**:
- `EnviarDocumentoAssinaturaAsync()` - Envia documento PDF para assinatura eletrônica
- `ObterEnvelopeAsync()` - Obtém status e detalhes de envelope específico
- `ListarEnvelopesAsync()` - Lista todos os envelopes dos últimos 30 dias

**Recursos**:
- Autenticação via Bearer Token no header
- Suporte a múltiplos signatários
- Configuração de posição de assinatura no documento

#### Microsoft Graph API
**Implementação**: `MicrosoftGraphService.cs`

**Métodos implementados**:
- `ObterUsuariosAsync()` - Lista usuários do Azure AD
- `EnviarEmailAsync()` - Envia email via Microsoft Graph
- `ObterGruposAsync()` - Lista grupos do Azure AD

**Recursos**:
- Autenticação via ClientSecretCredential
- Suporte a conteúdo HTML em emails
- Integração completa com Microsoft 365

## Configuração e Execução

### Pré-requisitos

- .NET 8 SDK
- Docker e Docker Compose (para execução containerizada)
- MongoDB (se executar localmente sem Docker)

### Configuração de Credenciais

Edite o arquivo `src/MagmaAssessment.API/appsettings.json`:

```json
{
  "ConnectionStrings": {
    "MongoDB": "mongodb://root:example@localhost:27017/magma_assessment?authSource=admin"
  },
  "Force1": {
    "BaseUrl": "https://api.magma-3.com/v2/Force1",
    "Username": "seu_usuario",
    "Password": "sua_senha",
    "Enterprise": "sua_empresa"
  },
  "GoogleMaps": {
    "ApiKey": "sua_chave_google_maps"
  },
  "DocuSign": {
    "BasePath": "https://demo.docusign.net/restapi",
    "AccessToken": "seu_access_token",
    "AccountId": "seu_account_id"
  },
  "MicrosoftGraph": {
    "TenantId": "seu_tenant_id",
    "ClientId": "seu_client_id",
    "ClientSecret": "seu_client_secret"
  }
}
```

### Execução com Docker (Recomendado)

```bash
# Iniciar todos os serviços
docker-compose up -d

# Verificar logs
docker-compose logs -f api

# Acessar aplicação
# API: http://localhost:5000
# Swagger: http://localhost:5000/swagger
# Mongo Express: http://localhost:8081 (user: admin, pass: pass)

# Parar serviços
docker-compose down
```

### Execução Local

```bash
# Restaurar dependências
dotnet restore

# Executar API
cd src/MagmaAssessment.API
dotnet run

# Acessar aplicação
# API: http://localhost:5059
# Swagger: http://localhost:5059/swagger
```

## Testes da API

### Health Check

```bash
curl http://localhost:5059/health
```

### Questão 1 - Force1

```bash
# Listar todos os ativos
curl http://localhost:5059/ativos

# Listar computadores inativos
curl http://localhost:5059/computadores-inativos

# Buscar ativo por ID
curl http://localhost:5059/ativo/758308234698
```

### Questão 2 - Produtos

```bash
# Listar produtos
curl http://localhost:5059/api/produtos

# Criar produto
curl -X POST http://localhost:5059/api/produtos \
  -H "Content-Type: application/json" \
  -d '{
    "nome": "Produto Teste",
    "preco": 150.00
  }'

# Buscar produto por ID
curl http://localhost:5059/api/produtos/1

# Atualizar produto
curl -X PUT http://localhost:5059/api/produtos/1 \
  -H "Content-Type: application/json" \
  -d '{
    "id": 1,
    "nome": "Produto Atualizado",
    "preco": 200.00
  }'

# Deletar produto
curl -X DELETE http://localhost:5059/api/produtos/1
```

### Questão 3 - Clientes (MongoDB)

```bash
# Listar clientes
curl http://localhost:5059/api/clientes

# Criar cliente
curl -X POST http://localhost:5059/api/clientes \
  -H "Content-Type: application/json" \
  -d '{
    "nome": "João Silva",
    "email": "joao.silva@example.com",
    "telefone": "11987654321"
  }'

# Buscar cliente por ID
curl http://localhost:5059/api/clientes/{id}

# Buscar por email
curl http://localhost:5059/api/clientes/email/joao.silva@example.com

# Atualizar cliente
curl -X PUT http://localhost:5059/api/clientes/{id} \
  -H "Content-Type: application/json" \
  -d '{
    "nome": "João Silva Atualizado",
    "email": "joao.silva@example.com",
    "telefone": "11999999999"
  }'

# Deletar cliente
curl -X DELETE http://localhost:5059/api/clientes/{id}
```

## Estrutura do Docker

### Containers

**api**: Aplicação .NET 8 (porta 5000)
**mongodb**: Banco de dados MongoDB (porta 27017)
**mongo-express**: Interface web MongoDB (porta 8081)

### Volumes

- `mongodb_data`: Persistência dos dados do MongoDB

### Networks

- `magma_network`: Rede isolada bridge para comunicação entre containers

### Health Checks

Todos os containers possuem health checks configurados:
- MongoDB: Verificação a cada 10s via mongosh
- API: Verificação a cada 30s no endpoint /health

## Logging

Sistema de logging estruturado com Serilog:

**Console**: Logs em tempo real durante execução
**Arquivo**: `logs/magma-assessment-YYYYMMDD.txt` com rotação diária

Níveis de log:
- Information: Operações normais
- Warning: Situações anormais não críticas
- Error: Erros que requerem atenção
- Fatal: Erros críticos que causam shutdown

## Segurança

Implementações de segurança:
- Variáveis de ambiente para credenciais sensíveis
- Connection strings não versionadas no .gitignore
- Validações de entrada com Data Annotations
- MongoDB com autenticação habilitada
- Logs com informações sensíveis mascaradas
- HTTPS redirect configurado
- Docker containers com usuário não-root

## Monitoramento

**Health Check Endpoint**: `/health`

Retorna status da aplicação:
```json
{
  "status": "healthy",
  "timestamp": "2025-11-11T10:30:00Z",
  "service": "Magma3 Backend Assessment",
  "environment": "Production",
  "version": "1.0.0"
}
```

**MongoDB UI**: http://localhost:8081 (Mongo Express)
- Usuário: admin
- Senha: pass

## Observações Técnicas

### Resilience Patterns

A integração com a API Force1 implementa os seguintes patterns de resiliência:

**Retry Policy**: 3 tentativas com exponential backoff (2^retry segundos)
**Circuit Breaker**: Abre após 3 falhas consecutivas, permanece aberto por 1 minuto
**Fallback**: Retorna dados mock quando circuit breaker está aberto ou API indisponível

### Thread Safety

O repositório de produtos utiliza ConcurrentDictionary para garantir operações thread-safe em ambiente de múltiplas requisições concorrentes.

### Async/Await

Todas as operações de I/O (banco de dados, APIs externas, leitura de arquivos) utilizam async/await para melhor performance e escalabilidade.

### Validações

Implementadas com Data Annotations nos modelos:
- Required: Campos obrigatórios
- StringLength: Tamanho mínimo e máximo de strings
- EmailAddress: Validação de formato de email
- Range: Validação de intervalos numéricos

## Autor

**Bruno Henrique**
- Email: brunoricksp@gmail.com
- GitHub: github.com/brunohenss
- LinkedIn: linkedin.com/in/bruno-henrique

## Licença

Projeto desenvolvido para avaliação técnica no processo seletivo da Magma3.
