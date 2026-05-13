# AEMS - Academic Event Management System

AEMS is an advanced event management platform tailored for academic environments. It handles end-to-end event lifecycles, from proposal and budgeting to ticketing, real-time check-ins, and AI-driven feedback analysis.

## 🚀 Key Technical Highlights
* **AI-Powered Chatbot & RAG:** Integrated a custom Python-based RAG (Retrieval-Augmented Generation) backend to power an intelligent chatbot for user assistance.
* **Sentiment Analysis with PhoBERT:** Utilized a fine-tuned PhoBERT deep learning model to automatically analyze and categorize student feedback sentiments.
* **Real-time Operations:** Implemented **SignalR** for instant notifications, live chat features, and real-time check-in syncing.
* **Robust Backend Architecture:** Developed using ASP.NET Core MVC with a clean **N-Tier Architecture** (DataAccess, BusinessLogic, Presentation layers).
* **Comprehensive Workflows:** Engineered complex modules including Event Waitlists, Budget Proposals with multi-level approvals, and Dynamic Quiz generation.

## 🛠 Tech Stack
* **Web Framework:** ASP.NET Core MVC (.NET 7/8)
* **Language:** C#, Python
* **Database:** SQL Server & Entity Framework Core
* **Real-time & AI:** SignalR, FastAPI (Python), PhoBERT, FAISS (Vector DB for RAG)
* **Frontend:** HTML, CSS, JavaScript, Bootstrap

## 🏗 System Architecture
1.  **AEMS_Solution:** The main ASP.NET Core MVC presentation and API routing layer.
2.  **BusinessLogic:** Encapsulates core services, AutoMapper profiles, DTOs, and role-based validations.
3.  **DataAccess:** Manages EF Core DbContext, Repositories, and Unit of Work patterns.
4.  **Python (AI Microservices):** Houses the PhoBERT model training scripts and the RAG-based FastAPI server.

## 📥 Getting Started
1. Clone the repository:
   ```bash
   git clone [https://github.com/huynh5079/AEMS.git](https://github.com/huynh5079/AEMS.git)
2. Setup the SQL Server connection string in `appsettings.json`.
3. Apply Entity Framework migrations:
```bash
Update-Database
4. *Optional:* Run the Python backend services for Chatbot and Sentiment Analysis (see `/Python/rag-backend/README_RAG.md`).
5. Run the `AEMS_Solution` project.

