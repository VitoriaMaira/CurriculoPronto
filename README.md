# Currículo Pronto

> Plataforma web para criação de currículos profissionais em PDF, com processamento assíncrono, acompanhamento em tempo real e arquitetura distribuída com .NET e Angular.

## Sobre o projeto

O **Currículo Pronto** é um projeto de estudos e portfólio criado para aplicar conceitos modernos de desenvolvimento back-end e front-end em um fluxo completo de negócio.

A aplicação permite que o usuário:

1. realize autenticação;
2. preencha os dados profissionais;
3. solicite a geração do currículo;
4. acompanhe o processamento em tempo real;
5. baixe o arquivo em PDF quando estiver pronto.

A geração do documento não acontece diretamente durante a requisição HTTP. A API registra a solicitação e envia uma mensagem para uma fila. Um Worker processa o currículo em segundo plano, gera o PDF, armazena o arquivo temporariamente e notifica o front-end quando o processamento termina.

Esse fluxo foi escolhido para estudar uma arquitetura próxima de cenários reais, nos quais tarefas mais pesadas não devem bloquear a experiência do usuário.

---

## Objetivos de aprendizagem

O projeto foi desenvolvido para consolidar conhecimentos em:

- C# e ASP.NET Core;
- Angular e TypeScript;
- arquitetura em camadas;
- princípios de Clean Architecture;
- Domain-Driven Design em nível introdutório;
- APIs REST;
- processamento assíncrono;
- mensageria com RabbitMQ;
- comunicação em tempo real com SignalR;
- persistência com Entity Framework Core;
- SQL Server;
- cache e armazenamento temporário com Redis;
- geração de PDF com QuestPDF;
- autenticação com Google e JWT;
- injeção de dependência;
- testes automatizados;
- observabilidade com .NET Aspire;
- containers e orquestração do ambiente local.

---

## Problema que o sistema resolve

Criar um currículo bem estruturado pode ser difícil para pessoas que não sabem como organizar suas experiências, formações e habilidades em um documento profissional.

O **Currículo Pronto** centraliza essas informações em um formulário e transforma os dados em um PDF padronizado, reduzindo o trabalho manual do usuário.

Além da entrega funcional, o projeto demonstra como dividir uma aplicação em responsabilidades claras e como integrar diferentes serviços sem concentrar toda a lógica em uma única API.

---

## Arquitetura

O back-end foi separado nos seguintes projetos:

```text
src/
├── CurriculoPronto.Api/
├── CurriculoPronto.AppHost/
├── CurriculoPronto.Application/
├── CurriculoPronto.Contracts/
├── CurriculoPronto.Domain/
├── CurriculoPronto.Infrastructure/
├── CurriculoPronto.ServiceDefaults/
└── CurriculoPronto.Worker/
```

### Responsabilidade de cada projeto

#### `CurriculoPronto.Domain`

Contém o núcleo do negócio:

- entidades;
- enums;
- regras de domínio;
- transições de status;
- exceções de negócio.

O domínio não depende de banco de dados, RabbitMQ, Redis, ASP.NET Core ou qualquer outra tecnologia externa.

#### `CurriculoPronto.Application`

Organiza os casos de uso da aplicação:

- solicitar geração de currículo;
- consultar o andamento de um job;
- processar a geração;
- baixar o currículo;
- autenticar o usuário;
- validar se o currículo pertence ao usuário autenticado.

Também contém as abstrações que serão implementadas pela infraestrutura, como repositórios, armazenamento de arquivos, gerador de PDF e publicadores de mensagens.

#### `CurriculoPronto.Contracts`

Centraliza os contratos de comunicação:

- requests e responses da API;
- DTOs;
- mensagens do RabbitMQ;
- eventos enviados pelo SignalR.

Esse projeto define o formato dos dados compartilhados entre os componentes, mas não contém regras de negócio.

#### `CurriculoPronto.Infrastructure`

Implementa as integrações externas:

- Entity Framework Core;
- SQL Server;
- RabbitMQ;
- Redis;
- QuestPDF;
- validação do token do Google;
- emissão de JWT;
- repositórios.

A infraestrutura conhece a Application e implementa as interfaces definidas por ela.

#### `CurriculoPronto.Api`

É a porta de entrada HTTP utilizada pelo Angular.

Responsabilidades:

- receber requisições;
- validar autenticação e autorização;
- chamar os casos de uso;
- retornar respostas HTTP;
- disponibilizar o Hub do SignalR;
- entregar o PDF para download;
- consumir eventos de conclusão quando necessário.

Os controllers devem permanecer pequenos e não concentrar regras de negócio.

#### `CurriculoPronto.Worker`

Executa o processamento em segundo plano.

O Worker:

1. consome uma mensagem do RabbitMQ;
2. consulta e atualiza o job;
3. gera o currículo com QuestPDF;
4. salva o PDF temporariamente no Redis;
5. marca o job como concluído;
6. publica o evento de conclusão.

#### `CurriculoPronto.AppHost`

Orquestra o ambiente com .NET Aspire.

Ele conecta:

- API;
- Worker;
- SQL Server;
- RabbitMQ;
- Redis;
- aplicação Angular.

#### `CurriculoPronto.ServiceDefaults`

Centraliza configurações comuns do Aspire:

- health checks;
- logs;
- métricas;
- traces;
- service discovery;
- resiliência de chamadas HTTP.

---

## Direção das dependências

```text
API ───────────────┐
                   ├──> Application ───> Domain
Worker ────────────┘

Infrastructure ───────> Application
Infrastructure ───────> Domain
Application ──────────> Contracts
API e Worker ─────────> Infrastructure
```

A regra principal é que o núcleo do sistema não depende das tecnologias externas.

Isso ajuda a manter:

- baixo acoplamento;
- responsabilidades claras;
- código mais testável;
- facilidade para substituir implementações;
- menor impacto de mudanças.

---

## Fluxo principal

```text
Angular
   |
   | POST /api/resumes
   v
API
   |
   | cria o Job no SQL Server
   | publica ResumeGenerationRequested
   v
RabbitMQ
   |
   v
Worker
   |
   | gera o PDF com QuestPDF
   | armazena o arquivo no Redis
   | atualiza o Job no SQL Server
   | publica ResumeGenerationCompleted
   v
API / SignalR
   |
   | envia ResumeReady
   v
Angular
   |
   | GET /api/resumes/{jobId}/download
   v
Download do PDF
```

---

## Processamento assíncrono

A geração do PDF é tratada como um processo assíncrono.

Em vez de manter a requisição HTTP aberta durante toda a geração, a API responde rapidamente com o identificador do job.

Exemplo de resposta:

```json
{
  "jobId": "8ed648e9-b62a-4cb7-b2cf-d72df7d94f21",
  "status": "Queued"
}
```

O processamento continua no Worker.

### Benefícios

- a API não fica bloqueada;
- o usuário recebe uma resposta rápida;
- o Worker pode processar tarefas separadamente;
- falhas podem ser tratadas com retry;
- a aplicação pode evoluir para múltiplos consumidores;
- o processamento fica mais resiliente.

---

## Status do job

O processo de geração utiliza estados para representar seu andamento:

```text
Queued -> Processing -> Completed
                    └-> Failed
Completed -> Expired
```

- `Queued`: solicitação registrada e aguardando processamento;
- `Processing`: o Worker iniciou a geração;
- `Completed`: o PDF foi gerado com sucesso;
- `Failed`: ocorreu uma falha;
- `Expired`: o arquivo temporário não está mais disponível.

As mudanças de estado pertencem à entidade de domínio, evitando alterações livres e inconsistentes.

---

## RabbitMQ

O RabbitMQ é utilizado para desacoplar a API do Worker.

### Mensagens principais

#### `ResumeGenerationRequested`

Publicada pela API quando o usuário solicita um currículo.

```json
{
  "jobId": "8ed648e9-b62a-4cb7-b2cf-d72df7d94f21",
  "userId": "81fd45c6-5471-4f2d-88c8-2e03921aa198",
  "templateKey": "default",
  "resume": {}
}
```

#### `ResumeGenerationCompleted`

Publicada pelo Worker após a geração.

```json
{
  "jobId": "8ed648e9-b62a-4cb7-b2cf-d72df7d94f21",
  "userId": "81fd45c6-5471-4f2d-88c8-2e03921aa198",
  "expiresAt": "2026-07-29T14:00:00Z"
}
```

### Conceitos aplicados

- producer e consumer;
- filas;
- mensagens;
- ACK e NACK;
- retry;
- idempotência;
- Dead Letter Queue;
- processamento eventual;
- desacoplamento entre serviços.

---

## SignalR

O SignalR permite que o Angular seja avisado quando o currículo estiver pronto.

Sem SignalR, o front-end precisaria consultar a API repetidamente para descobrir se o processamento terminou.

Com a comunicação em tempo real:

1. o Angular abre uma conexão com o Hub;
2. entra no grupo correspondente ao job;
3. o back-end envia o evento `ResumeReady`;
4. o front-end atualiza a interface;
5. o botão de download é liberado.

### Conceitos aplicados

- WebSocket;
- conexão persistente;
- eventos em tempo real;
- grupos;
- autenticação da conexão;
- atualização reativa da interface.

---

## Redis

O Redis armazena temporariamente os bytes do PDF.

Exemplo de chave:

```text
resume:{jobId}
```

O arquivo possui um TTL, como 24 horas.

### Por que usar Redis?

O SQL Server mantém o histórico e os metadados do job. O Redis mantém o arquivo temporário.

As duas tecnologias possuem responsabilidades diferentes:

```text
SQL Server -> histórico, usuário, status e timestamps
Redis      -> arquivo PDF temporário
```

Quando o TTL termina, o arquivo é removido automaticamente. O registro do job pode continuar no SQL Server para informar que o currículo foi gerado, mas expirou.

---

## SQL Server e Entity Framework Core

O SQL Server armazena informações permanentes da aplicação.

Principais entidades:

- `User`;
- `ResumeJob`.

Exemplo de dados de um job:

```text
Id
UserId
TemplateKey
Status
CreatedAt
StartedAt
CompletedAt
ExpiresAt
ErrorCode
ErrorMessage
```

### Conceitos aplicados

- ORM;
- `DbContext`;
- migrations;
- mapeamento de entidades;
- chaves primárias e estrangeiras;
- repositórios;
- Unit of Work;
- consultas assíncronas;
- persistência de estados.

---

## Autenticação e autorização

O projeto utiliza login com Google.

Fluxo planejado:

1. o Angular recebe um ID token do Google;
2. envia o token para a API;
3. a API valida o token;
4. busca ou cria o usuário;
5. gera um JWT próprio;
6. o Angular usa o JWT nas próximas requisições.

A aplicação também valida se o currículo pertence ao usuário autenticado antes de permitir:

- consulta do job;
- entrada no grupo do SignalR;
- download do PDF.

### Conceitos aplicados

- autenticação;
- autorização;
- claims;
- JWT;
- Bearer Token;
- validação de audiência;
- ownership de recurso;
- proteção de endpoints;
- segurança da conexão SignalR.

---

## Front-end Angular

O Angular será responsável por:

- autenticação;
- formulário de currículo;
- validação dos campos;
- envio dos dados;
- acompanhamento do job;
- conexão SignalR;
- apresentação dos estados;
- download do PDF.

Estrutura sugerida:

```text
src/app/
├── core/
│   ├── auth/
│   ├── guards/
│   ├── interceptors/
│   └── services/
├── features/
│   ├── authentication/
│   └── resumes/
├── shared/
│   ├── components/
│   ├── models/
│   └── validators/
└── app.routes.ts
```

### Conceitos aplicados no Angular

- standalone components;
- TypeScript;
- services;
- injeção de dependência;
- Reactive Forms;
- `FormGroup`;
- `FormArray`;
- validações;
- RxJS;
- Signals;
- interceptors;
- route guards;
- lazy loading;
- gerenciamento de estado de tela;
- download de arquivos com `Blob`;
- integração com SignalR.

---

## Reactive Forms

O formulário do currículo possui campos simples e listas dinâmicas.

Exemplos de listas:

- experiências profissionais;
- formações;
- habilidades;
- idiomas.

Essas seções podem ser implementadas com `FormArray`.

```typescript
experiences = this.formBuilder.array([]);
```

O usuário pode adicionar ou remover itens sem recarregar a página.

---

## Geração do PDF

O Worker utiliza QuestPDF para transformar os dados enviados em um documento.

Responsabilidades do gerador:

- definir o layout;
- criar cabeçalho;
- exibir informações pessoais;
- organizar experiências;
- organizar formações;
- listar habilidades;
- aplicar estilos;
- retornar os bytes do arquivo.

A geração fica isolada por uma interface:

```csharp
public interface IResumePdfGenerator
{
    byte[] Generate(ResumeContentDto resume);
}
```

Isso permite testar a Application sem depender diretamente do QuestPDF.

---

## Resiliência

Como o projeto possui mensageria e serviços externos, falhas precisam ser consideradas.

Cenários:

- RabbitMQ indisponível;
- Redis indisponível;
- falha ao gerar o PDF;
- mensagem processada mais de uma vez;
- Worker interrompido;
- token expirado;
- arquivo expirado;
- conexão SignalR perdida.

Estratégias planejadas:

- retry controlado;
- Dead Letter Queue;
- idempotência;
- logs estruturados;
- atualização do job para `Failed`;
- reconexão do SignalR;
- mensagens de erro claras;
- health checks.

---

## Observabilidade

O .NET Aspire fornece uma visão centralizada dos componentes.

O projeto utiliza ou prevê:

- logs estruturados;
- traces distribuídos;
- métricas;
- health checks;
- correlação por `jobId`;
- acompanhamento da API;
- acompanhamento do Worker;
- visualização dos recursos de infraestrutura.

O `jobId` funciona como identificador de correlação do fluxo:

```text
requisição HTTP
-> criação no banco
-> mensagem no RabbitMQ
-> processamento no Worker
-> gravação no Redis
-> evento SignalR
-> download
```

---

## Testes

A estratégia de testes está dividida por responsabilidade.

### Domain

- criação de job;
- transições válidas de status;
- proteção contra estados inconsistentes.

### Application

- criação de solicitação;
- publicação da mensagem correta;
- validação de ownership;
- tratamento de arquivo expirado;
- processamento com dependências falsas.

### Infrastructure

- persistência com EF Core;
- integração com SQL Server;
- integração com Redis;
- publicação e consumo no RabbitMQ;
- geração real do PDF.

### API

- autenticação;
- autorização;
- status HTTP;
- contratos de entrada e saída;
- download;
- integração dos endpoints.

### Worker

- consumo da mensagem;
- atualização de status;
- geração;
- tratamento de falha;
- ACK, retry e DLQ.

### Angular

- formulários;
- validators;
- services;
- interceptor;
- guard;
- estados da tela;
- eventos do SignalR.

---

## Tecnologias

### Back-end

- .NET;
- C#;
- ASP.NET Core;
- Entity Framework Core;
- SQL Server;
- RabbitMQ;
- Redis;
- SignalR;
- QuestPDF;
- JWT;
- Google Identity Services;
- .NET Aspire;
- OpenTelemetry.

### Front-end

- Angular;
- TypeScript;
- RxJS;
- Angular Signals;
- Reactive Forms;
- SignalR Client.

### Qualidade e infraestrutura

- Docker;
- testes unitários;
- testes de integração;
- Testcontainers;
- health checks;
- logs, métricas e traces.

---

## Como executar

> Esta seção deve ser atualizada conforme a implementação avançar.

### Pré-requisitos

- .NET SDK;
- Node.js;
- Angular CLI;
- Docker;
- Git.

### Back-end

```bash
dotnet restore
dotnet build
dotnet run --project src/CurriculoPronto.AppHost
```

O AppHost deverá iniciar:

- API;
- Worker;
- SQL Server;
- RabbitMQ;
- Redis;
- dashboard do Aspire.

### Front-end

```bash
cd web/curriculo-pronto-web
npm install
ng serve
```

---

## Configuração de segredos

Credenciais e chaves não devem ser versionadas.

Exemplos:

- Google Client ID;
- chave de assinatura do JWT;
- connection strings externas;
- credenciais de produção.

No ambiente local, utilize `dotnet user-secrets` ou variáveis de ambiente.

```bash
dotnet user-secrets init
dotnet user-secrets set "Authentication:Google:ClientId" "SEU_CLIENT_ID"
dotnet user-secrets set "Authentication:Jwt:Key" "SUA_CHAVE_SEGURA"
```

---

## Roadmap

- [ ] Criar solução e referências entre projetos;
- [ ] Configurar AppHost e ServiceDefaults;
- [ ] Subir SQL Server, RabbitMQ e Redis;
- [ ] Modelar `User` e `ResumeJob`;
- [ ] Configurar EF Core e migrations;
- [ ] Criar casos de uso iniciais;
- [ ] Criar endpoints de criação e consulta;
- [ ] Publicar mensagem de geração;
- [ ] Consumir mensagem no Worker;
- [ ] Gerar PDF com QuestPDF;
- [ ] Armazenar PDF no Redis;
- [ ] Atualizar status no SQL Server;
- [ ] Notificar conclusão com SignalR;
- [ ] Implementar download;
- [ ] Criar autenticação com Google;
- [ ] Proteger recursos por usuário;
- [ ] Construir o Angular;
- [ ] Adicionar retry e DLQ;
- [ ] Criar testes automatizados;
- [ ] Preparar documentação e demonstração.

---

## Decisões técnicas

### Por que não gerar o PDF diretamente na API?

Porque a geração pode levar tempo e não deve manter a requisição HTTP bloqueada.

### Por que usar RabbitMQ?

Para separar o recebimento da solicitação do processamento pesado.

### Por que usar um Worker?

Para executar tarefas em segundo plano com responsabilidade própria.

### Por que usar Redis?

Para manter o PDF temporariamente sem armazenar arquivos grandes no banco relacional.

### Por que manter o histórico no SQL Server?

Porque status, usuário e timestamps são dados permanentes e relacionais.

### Por que usar SignalR?

Para informar o resultado ao usuário sem polling constante.

### Por que separar Domain, Application e Infrastructure?

Para reduzir acoplamento, melhorar os testes e impedir que as regras do sistema dependam diretamente de frameworks.

---

## Aprendizados demonstrados

Este projeto demonstra capacidade de:

- transformar um problema de negócio em uma solução técnica;
- separar responsabilidades;
- criar uma API REST;
- aplicar orientação a objetos;
- utilizar abstrações e injeção de dependência;
- modelar entidades e estados;
- integrar banco de dados;
- trabalhar com filas;
- criar processamento assíncrono;
- implementar comunicação em tempo real;
- integrar back-end e Angular;
- pensar em falhas e segurança;
- documentar decisões técnicas.

O objetivo não é apenas reunir tecnologias, mas utilizar cada uma para resolver um problema específico dentro do fluxo.

---

## Status do projeto

**Em desenvolvimento para estudos e portfólio.**

A implementação está sendo feita de forma incremental, com cada etapa validada antes da introdução de uma nova tecnologia.

Essa abordagem permite compreender o papel de cada componente, em vez de criar toda a estrutura de uma vez sem acompanhar o funcionamento.

---

## Autoria

Projeto desenvolvido por **Maíra** como parte dos estudos em desenvolvimento full stack com **C#/.NET e Angular**.

---

## Licença

Este projeto é destinado a estudos e portfólio. A licença poderá ser definida posteriormente.
