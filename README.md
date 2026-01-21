# NotificationsAPI - Microserviço de Notificações

## 🚀 Quick Start

### Deploy no Kubernetes (COMPLETO)

```powershell
# 1. Navegar para a pasta do projeto
cd NotificationsAPI

# 2. Build da imagem Docker
docker build -t notificationsapi:latest .

# 3. Deploy no Kubernetes (ordem correta)
cd ..
cd k8s

# RabbitMQ
kubectl apply -f rabbitmq-deployment.yaml

# NotificationsAPI
kubectl apply -f notifications-deployment.yaml

# 4. Verificar status
kubectl get pods
kubectl logs -f deployment/notifications-api

# 5. Testar Health Check
kubectl port-forward service/notifications-api 8081:80
```

### 📦 Deploy Automático

Use o script PowerShell para deploy completo:

```powershell
# Deploy completo (inclui RabbitMQ)
.\k8s\deploy-all.ps1

# Verificar logs
kubectl logs -f deployment/notifications-api

# Acessar RabbitMQ Management UI
kubectl port-forward service/rabbitmq 15672:15672
# Acesse: http://localhost:15672
# Login: fiap / fiap123
```

---

## 🔌 Endpoints

- **Health**: http://localhost:8081/health
- **RabbitMQ Management**: http://localhost:15672 (user: `fiap`, pass: `fiap123`)

---

## ⚙️ Configuração

### Variáveis de Ambiente
O .NET Configuration usa `__` (double underscore) para hierarquia de seções:

```sh
# RabbitMQ
RABBITMQ__HOSTNAME=rabbitmq
RABBITMQ__PORT=5672
RABBITMQ__USERNAME=fiap
RABBITMQ__PASSWORD=fiap123
```

### Mapeamento no Código

```csharp
// Program.cs lê assim:
builder.Configuration.GetSection("RabbitMQ").Get<RabbitMqSettings>()
// → RABBITMQ__HOSTNAME
// → RABBITMQ__PORT
// → RABBITMQ__USERNAME
// → RABBITMQ__PASSWORD
```

---

## ☸️ Kubernetes

### Recursos Aplicados

```
# RabbitMQ
Deployment: rabbitmq                (RabbitMQ 3 com Management)
Service:    rabbitmq                (ClusterIP, portas 5672 + 15672)

# NotificationsAPI
Deployment: notifications-api       (2 réplicas)
Service:    notifications-api       (ClusterIP, porta 80)
```

### Portas Configuradas
- **NotificationsAPI Container**: 80
- **NotificationsAPI Service**: 80 (ClusterIP interno)
- **RabbitMQ AMQP**: 5672 (protocolo de mensageria)
- **RabbitMQ Management**: 15672 (UI web)
- **Port-forward API**: `kubectl port-forward service/notifications-api 8081:80`
- **Port-forward RabbitMQ**: `kubectl port-forward service/rabbitmq 15672:15672`

---

## 📨 RabbitMQ Integration

### Consumers Configurados

#### 1. UserCreatedConsumer
- **Queue**: `notifications-user-created`
- **Event**: `UserCreatedEvent`
- **Ação**: Envia email de boas-vindas (simulado via log)

#### 2. PaymentProcessedConsumer
- **Queue**: `notifications-payment-processed`
- **Event**: `PaymentProcessedEvent`
- **Ação**: 
  - Se `Status == "Approved"` → Envia email de confirmação de compra
  - Se `Status == "Rejected"` → Loga rejeição do pagamento

### Configuração MassTransit

```csharp
// Retry Policy
cfg.UseMessageRetry(r =>
{
    r.Exponential(5, TimeSpan.FromSeconds(3), TimeSpan.FromMinutes(2), TimeSpan.FromSeconds(3));
});

// Endpoints
cfg.ReceiveEndpoint("notifications-payment-processed", e =>
{
    e.ConfigureConsumer<PaymentProcessedConsumer>(context);
});

cfg.ReceiveEndpoint("notifications-user-created", e =>
{
    e.ConfigureConsumer<UserCreatedConsumer>(context);
});
```

### Testar Integração com RabbitMQ

#### 1. Acessar RabbitMQ Management UI

```sh
kubectl port-forward service/rabbitmq 15672:15672
# Acesse: http://localhost:15672
# Login: fiap / fiap123
```

#### 2. Publicar Mensagem de Teste - UserCreatedEvent
Na UI do RabbitMQ:
1. Vá em **Queues** → `notifications-user-created`
2. Clique em **Publish message**
3. Configure:
   - **Content type**: `application/vnd.masstransit+json`
   - **Delivery mode**: `2 (persistent)`
4. Cole o payload:

```json
{
  "messageId": "00000000-0000-0000-0000-000000000001",
  "conversationId": "00000000-0000-0000-0000-000000000002",
  "sourceAddress": "rabbitmq://rabbitmq/user_api",
  "destinationAddress": "rabbitmq://rabbitmq/notifications-user-created",
  "messageType": [
    "urn:message:Shared.Events:UserCreatedEvent"
  ],
  "message": {
    "userId": "550e8400-e29b-41d4-a716-446655440001",
    "email": "user@example.com"
  },
  "sentTime": "2026-01-20T19:00:00Z"
}
```

#### 3. Publicar Mensagem de Teste - PaymentProcessedEvent
1. Vá em **Queues** → `notifications-payment-processed`
2. Clique em **Publish message**
3. Cole o payload:

```json
{
  "messageId": "00000000-0000-0000-0000-000000000003",
  "conversationId": "00000000-0000-0000-0000-000000000004",
  "sourceAddress": "rabbitmq://rabbitmq/payment_api",
  "destinationAddress": "rabbitmq://rabbitmq/notifications-payment-processed",
  "messageType": [
    "urn:message:Shared.Events:PaymentProcessedEvent"
  ],
  "message": {
    "orderId": "550e8400-e29b-41d4-a716-446655440005",
    "userId": "550e8400-e29b-41d4-a716-446655440006",
    "gameId": "550e8400-e29b-41d4-a716-446655440007",
    "price": 299.90,
    "status": "Approved"
  },
  "sentTime": "2026-01-20T19:30:00Z"
}
```

#### 4. Verificar Logs

```sh
kubectl logs -f deployment/notifications-api
```

**Saída esperada:**
```
[NotificationsAPI] Welcome email sent to user@example.com (UserId=550e8400-e29b-41d4-a716-446655440001)
[NotificationsAPI] Purchase approved for UserId=550e8400-e29b-41d4-a716-446655440006, GameId=550e8400-e29b-41d4-a716-446655440007, Price=299.90
[NotificationsAPI] Purchase confirmation email sent to UserId=550e8400-e29b-41d4-a716-446655440006
```

---

## 🧪 Testes

### Testar Health Check (Port-forward ativo)

```sh
# Health Check
curl http://localhost:8081/health

# Saída esperada:
# {"status":"healthy"}
```

### Testar Consumers

**Cenário 1: Usuário criado**
```sh
# Publicar UserCreatedEvent via RabbitMQ Management UI
# Verificar logs: kubectl logs -f deployment/notifications-api
# Log esperado: "[NotificationsAPI] Welcome email sent to user@example.com"
```

**Cenário 2: Pagamento aprovado**
```sh
# Publicar PaymentProcessedEvent com Status="Approved"
# Verificar logs
# Log esperado: 
# - "Purchase approved for UserId=..."
# - (após 2 segundos) "Purchase confirmation email sent to UserId=..."
```

**Cenário 3: Pagamento rejeitado**
```sh
# Publicar PaymentProcessedEvent com Status="Rejected"
# Log esperado: "Purchase rejected for UserId=..., GameId=..."
```

---

## 📁 Estrutura

```
NotificationsAPI/
├── Consumers/              # Message consumers
│   ├── UserCreatedConsumer.cs
│   └── PaymentProcessedConsumer.cs
├── Controllers/            # API endpoints
│   └── HealthController.cs
├── Infrastructure/         # Configuração
│   └── RabbitMqSettings.cs
├── Shared/                # Eventos compartilhados
│   └── Events/
│       ├── UserCreatedEvent.cs
│       └── PaymentProcessedEvent.cs
├── k8s/                   # Kubernetes manifests
│   ├── notifications-deployment.yaml
│   └── rabbitmq-deployment.yaml
├── Dockerfile             # Multi-stage build
├── Program.cs             # Configuração da aplicação
├── appsettings.json       # Configurações locais
└── README.md
```

---

## 🐛 Troubleshooting

### Pod não inicia (CrashLoopBackOff)

```sh
# Ver logs detalhados
kubectl logs -f deployment/notifications-api

# Verificar eventos
kubectl describe pod -l app=notifications-api

# Problemas comuns:
# 1. RabbitMQ não conecta → Verifique: kubectl get pods | grep rabbitmq
# 2. Filas não criadas → As filas são criadas automaticamente pelo MassTransit
# 3. Imagem não encontrada → Verifique: docker images | grep notificationsapi
```

### RabbitMQ não está rodando

```sh
# Verificar status
kubectl get pods -l app=rabbitmq

# Logs do RabbitMQ
kubectl logs -f deployment/rabbitmq

# Redeploy
kubectl delete -f k8s/rabbitmq-deployment.yaml
kubectl apply -f k8s/rabbitmq-deployment.yaml
```

### Mensagens não são consumidas

**Sintoma**: Mensagens publicadas no RabbitMQ não aparecem nos logs

**Diagnóstico:**
```sh
# 1. Verificar se as filas existem
# Acesse: http://localhost:15672 (após port-forward)
# Vá em "Queues" e procure por:
# - notifications-user-created
# - notifications-payment-processed

# 2. Verificar logs do NotificationsAPI
kubectl logs -f deployment/notifications-api

# 3. Verificar se o consumer está conectado
# No RabbitMQ Management UI: Queues → [nome da fila] → Consumers
```

**Solução:**
```sh
# Reiniciar o pod
kubectl delete pod -l app=notifications-api

# Verificar reconexão
kubectl logs -f deployment/notifications-api
```

### Erro: Cannot connect to RabbitMQ

**Causa**: NotificationsAPI inicia antes do RabbitMQ estar pronto

**Solução:**
```sh
# 1. Verificar se RabbitMQ está running
kubectl get pods -l app=rabbitmq

# 2. Aguardar RabbitMQ ficar pronto
kubectl wait --for=condition=ready pod -l app=rabbitmq --timeout=60s

# 3. Reiniciar NotificationsAPI
kubectl delete pod -l app=notifications-api
```

### Verificar Status Geral

```sh
# Status de todos os pods
kubectl get pods

# Logs NotificationsAPI
kubectl logs -f deployment/notifications-api

# Logs RabbitMQ
kubectl logs -f deployment/rabbitmq

# Remover tudo e redeployar
kubectl delete -f k8s/
kubectl apply -f k8s/rabbitmq-deployment.yaml
# Aguardar RabbitMQ ficar pronto
sleep 30
kubectl apply -f k8s/notifications-deployment.yaml
```

---

## 🔒 Segurança

- ✅ Container roda com configuração padrão do ASP.NET Core
- ✅ Configurações separadas do código
- ⚠️ Credenciais RabbitMQ são demo - **MUDE EM PRODUÇÃO**
- ⚠️ Usuário/senha padrão do RabbitMQ (fiap/fiap123 neste ambiente) - **Configure usuários dedicados em produção**
- ⚠️ RabbitMQ Management UI exposta - **Restrinja acesso em produção**

---

## 📝 Notas Técnicas

### MassTransit + RabbitMQ
- ✅ Usa MassTransit 8.0 para abstração do RabbitMQ
- ✅ Consumers: `UserCreatedConsumer`, `PaymentProcessedConsumer`
- ✅ Queues: `notifications-user-created`, `notifications-payment-processed`
- ✅ Retry Policy: Exponential backoff (5 tentativas, 3s → 2min)
- ✅ Filas criadas automaticamente pelo MassTransit

### Notificações Simuladas
- 📧 **Email de Boas-vindas**: Loga no console quando usuário é criado
- 📧 **Email de Confirmação**: Loga no console quando pagamento é aprovado (com delay de 2s)
- 📧 **Pagamento Rejeitado**: Loga no console quando pagamento é rejeitado

### Health Checks
- ✅ Endpoint básico: `/health`
- ⚠️ Não valida conectividade com RabbitMQ (use readiness probe para produção)

---

## 🚀 Melhorias para Produção

### Health Checks Avançados
Adicione health checks para RabbitMQ:

```csharp
// Program.cs
builder.Services.AddHealthChecks()
    .AddRabbitMQ(rabbitConnectionString: $"amqp://{rabbitMQSettings.UserName}:{rabbitMQSettings.Password}@{rabbitMQSettings.HostName}:{rabbitMQSettings.Port}");

app.MapHealthChecks("/health/live");
app.MapHealthChecks("/health/ready");
```

Habilite health checks no `notifications-deployment.yaml`:

```yaml
livenessProbe:
  httpGet:
    path: /health/live
    port: 80
  initialDelaySeconds: 30
  periodSeconds: 10

readinessProbe:
  httpGet:
    path: /health/ready
    port: 80
  initialDelaySeconds: 10
  periodSeconds: 5
```

### Notificações Reais
Substitua logs por envio real de emails:

```csharp
// UserCreatedConsumer.cs
public async Task Consume(ConsumeContext<UserCreatedEvent> context)
{
    var message = context.Message;
    
    // Enviar email via SendGrid, AWS SES, ou Azure Communication Services
    await _emailService.SendWelcomeEmailAsync(message.Email, message.UserId);
    
    logger.LogInformation("Welcome email sent to {Email}", message.Email);
}
```

### Secrets Management
- Use Azure Key Vault ou HashiCorp Vault
- Credenciais RabbitMQ em Kubernetes Secrets
- Rotação automática de senhas

### Observabilidade
- Adicione Application Insights ou Prometheus
- Configure logs estruturados (Serilog)
- Implemente tracing distribuído (OpenTelemetry)
- Monitore métricas de consumo de mensagens

### Persistência de Mensagens
Configure RabbitMQ com volume persistente:

```yaml
# rabbitmq-deployment.yaml
spec:
  template:
    spec:
      volumes:
        - name: rabbitmq-data
          persistentVolumeClaim:
            claimName: rabbitmq-pvc
      containers:
        - name: rabbitmq
          volumeMounts:
            - name: rabbitmq-data
              mountPath: /var/lib/rabbitmq
```

### Dead Letter Queue (DLQ)
Configure DLQ para mensagens que falharam após todas as tentativas:

```csharp
cfg.ReceiveEndpoint("notifications-payment-processed", e =>
{
    e.ConfigureConsumer<PaymentProcessedConsumer>(context);
    
    // Configurar Dead Letter Exchange
    e.ConfigureDeadLetterQueueDeadLetterExchange();
    e.ConfigureDeadLetterQueueMessageTtl(TimeSpan.FromMinutes(10));
});
```

---

## 📚 Referências

- [MassTransit Documentation](https://masstransit.io/)
- [RabbitMQ Tutorials](https://www.rabbitmq.com/tutorials)
- [ASP.NET Core Health Checks](https://learn.microsoft.com/aspnet/core/host-and-deploy/health-checks)
- [Kubernetes Best Practices](https://kubernetes.io/docs/concepts/configuration/overview/)

---

## 📄 Licença

Este projeto é parte da Pós-Graduação FIAP e é fornecido como material educacional.
