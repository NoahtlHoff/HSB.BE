# HSB.BE - Chat API Backend

This project is an ASP.NET Core 8 Web API that serves as the backend for a chat application. It provides user authentication, chat functionalities with an AI, and conversation history management.

# Link to project

https://hackstreeboyswebsite20251023145048-dfagc5fzaqcufqfm.francecentral-01.azurewebsites.net/

## Key Features

*   **AI-Powered Chat**: A streaming chat endpoint that integrates with Azure OpenAI to provide responses from a language model.
*   **Semantic Search & Vector Embeddings:** Uses semantic search with vector embeddings to supply context-aware information to the AI model, improving relevance and accuracy of responses.
*   **Conversation History**: Stores and retrieves chat conversations, including conversation titles and individual messages, using Azure Cosmos DB.
*   **User Authentication**: Secure user registration and login using JWT (JSON Web Tokens).
*   **Database Integration**: Uses Entity Framework Core with a SQL Server for user management and other relational data.
*   **Email Service**: Includes a service to send emails, such as a welcome email upon registration.

## Technologies Used

*   [.NET 8](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)
*   [ASP.NET Core 8](https://learn.microsoft.com/en-us/aspnet/core/introduction-to-aspnet-core?view=aspnetcore-8.0)
*   [Entity Framework Core 8](https://learn.microsoft.com/en-us/ef/core/)
*   [Azure OpenAI](https://azure.microsoft.com/en-us/products/ai-services/openai-service) for AI chat and embeddings.
*   [Azure Cosmos DB](https://azure.microsoft.com/en-us/products/cosmos-db) for storing conversation data.
*   [SQL Server](https://www.microsoft.com/en-us/sql-server) for relational data (e.g., users).
*   [JWT (JSON Web Tokens)](https://jwt.io/) for authentication.
*   [Swagger](https://swagger.io/) for API documentation and testing.


## API Endpoints

### Authentication

*   `POST /api/auth/register`: Registers a new user.
*   `POST /api/auth/login`: Authenticates a user and returns a JWT.

### Chat

*   `POST /chat`: (Authentication required) Initiates a streaming chat session. The request body should be a `ChatRequestDto`.

### Conversations

*   `GET /api/conversations`: (Authentication required) Retrieves a list of all conversation names for the current user.
*   `GET /api/conversations/{conversationId}`: (Authentication required) Retrieves all messages for a specific conversation.
