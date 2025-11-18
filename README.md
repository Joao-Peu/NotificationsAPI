NotificationsAPI - Microservice for sending notifications (simulated)

This project implements the Notifications microservice in .NET 8 using DDD-like structure and RabbitMQ consumers.

Folders:
- NotificationsAPI: application project
- k8s: Kubernetes manifests for deployment and RabbitMQ

How it works:
- Listens to `UserCreatedQueue` and `PaymentProcessedQueue`
- Logs simulated emails to the console

Docker:
- Build the image from `NotificationsAPI/Dockerfile`

Configuration:
- Uses environment variables prefixed with `RABBITMQ__` to configure connection
