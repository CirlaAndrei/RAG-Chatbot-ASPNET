#  RAG Chatbot - ASP.NET Core Enterprise AI

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![.NET 8](https://img.shields.io/badge/.NET-8.0-512BD4)](https://dotnet.microsoft.com/)
[![Ollama](https://img.shields.io/badge/Ollama-Integrated-5B5B5B)](https://ollama.ai/)

A production-ready **Retrieval-Augmented Generation (RAG)** chatbot built with ASP.NET Core, featuring document upload, semantic search, and AI-powered conversations based on your own documents.


## ✨ Features

- ✅ **RAG Pipeline** - Complete Retrieval-Augmented Generation implementation
- ✅ **Document Upload** - Upload PDF and text files for custom knowledge bases
- ✅ **Semantic Search** - Vector embeddings with cosine similarity
- ✅ **Streaming Responses** - Real-time typing effect like ChatGPT
- ✅ **Document Management** - View, inspect, and delete uploaded documents
- ✅ **Multiple LLM Support** - Works with Ollama (local) and OpenAI
- ✅ **SQLite Persistence** - Documents persist between sessions
- ✅ **Modern UI** - Clean, responsive chat interface with document panel
- ✅ **Clean Architecture** - Separated into Core, Infrastructure, and API layers

## 🏗️ Architecture
RAGChatbot.sln
├── src/
│ ├── API/ # ASP.NET Core Web API
│ ├── Core/ # Domain entities and interfaces
│ └── Infrastructure/ # LLM providers, vector DB, document processing
└── tests/ # Unit tests

text

## 🚀 Getting Started

### Prerequisites

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download)
- [Ollama](https://ollama.ai/) (for local LLM) or OpenAI API key
- For Ollama, pull required models:
  ```bash
  ollama pull llama2
  ollama pull nomic-embed-text
Quick Start
Clone the repository

bash
git clone https://github.com/CirlaAndrei/RAG-Chatbot-ASPNET.git
cd RAG-Chatbot-ASPNET
Run the API

bash
cd src/API
dotnet run
Open the UI

Navigate to http://localhost:5162/index.html

Upload documents and start asking questions!

Configuration
Edit src/API/appsettings.json to configure:

json
{
  "LlmSettings": {
    "Provider": "ollama",  // or "openai"
    "Model": "llama2",
    "EmbeddingModel": "nomic-embed-text"
  }
}
📚 API Endpoints
Endpoint	Method	Description
/api/Chat/ask	POST	Ask a question (non-streaming)
/api/Chat/stream	POST	Ask a question with streaming response
/api/Document/upload	POST	Upload a document (PDF/TXT)
/api/Document/{id}	DELETE	Delete a document
/api/Debug/all-documents	GET	List all documents
/api/Debug/force-check	GET	Direct database inspection
🛠️ Built With
ASP.NET Core 8

Entity Framework Core

SQLite

Ollama

PdfPig

🎯 Use Cases
Personal Knowledge Base - Upload your documents and ask questions

Company Documentation - Internal wiki Q&A

Customer Support - Train on support articles

Legal Document Analysis - Contract clause extraction

Medical Research - Paper summarization

🤝 Contributing
Contributions are welcome! Feel free to submit a PR.

📄 License
This project is licensed under the MIT License.


Inspired by modern RAG architectures

📬 Contact
Andrei Cirla - GitHub

Project Link: https://github.com/CirlaAndrei/RAG-Chatbot-ASPNET
