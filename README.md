# 📦 DropShipX - Plataforma de E-Commerce & Dropshipping

> **Projeto Final:** Criação de API consumida por Website.

> **UC:** UC00605 - Programar para a web, na vertente servidor (server-side).

> **Autores:** Diogo Bilreiro & Gonçalo Gonçalves

![Status](https://img.shields.io/badge/Status-Concluído-success)
![Docker](https://img.shields.io/badge/Docker-Compose-blue)
![.NET](https://img.shields.io/badge/.NET-8.0-purple)

## 📖 Sobre o Projeto

O **DropShipX** é uma solução de comércio eletrónico desenhada para simular um modelo de negócio de **Dropshipping**. A aplicação permite que os utilizadores naveguem num catálogo, façam login e realizem encomendas.

A grande diferenciação técnica reside na gestão de stock: a loja não possui inventário local. Em vez disso, a API comunica em tempo real com um **simulador de fornecedor externo (WireMock)** para verificar a disponibilidade dos produtos, utilizando políticas de resiliência para garantir a robustez do sistema.

---

## 🏗️ Arquitetura do Sistema

O projeto foi desenvolvido utilizando uma arquitetura de microsserviços containerizados via **Docker Compose**.

🛠️ Stack Tecnológico

Backend: ASP.NET Core Web API (.NET 8) 


Base de Dados: MySQL 8.0 


Cache Distribuído: Redis 


Integração/Mock: WireMock (Simulação de API de Fornecedor) 


Resiliência: Polly (Retries & Circuit Breaker) 


Autenticação: JWT (JSON Web Tokens) 


Frontend: HTML5, CSS3, Bootstrap 5, JavaScript (Fetch API) 


DevOps: Docker & Docker Compose 

✨ Funcionalidades Principais

Autenticação Segura: Registo e Login de utilizadores com emissão de Tokens JWT.

Catálogo Otimizado: Listagem de produtos com imagens dinâmicas e Cache Redis para alta performance (reduzindo a carga na BD).

Gestão de Stock Externa: Endpoint especial que consulta o contentor WireMock para obter stock em tempo real.

Resiliência: Implementação de Polly para tentar reconectar automaticamente caso o fornecedor falhe.

Checkout: Criação de encomendas transacionais (Header + Detalhes) na base de dados MySQL.

Histórico: Área de cliente para consulta de encomendas passadas.

🚀 Como Executar o Projeto
Pré-requisitos
Docker Desktop instalado e a correr.

Passo a Passo
Clonar o Repositório:

Bash

git clone https://github.com/Bilreiro21/api-diogogoncalo-projeto-final.git
cd api-diogogoncalo-projeto-final
Arrancar os Serviços (Docker): Na raiz do projeto (onde está o docker-compose.yml), execute:

Bash

docker-compose up --build
Aguarde alguns instantes até que todos os contentores (API, MySQL, Redis, WireMock) estejam ativos.

Configurar a Base de Dados (Seed):

Aceda ao seu gestor de base de dados (ex: MySQL Workbench ou DBeaver).

Ligue-se ao servidor: localhost:3306 (User: root, Pass: password - conforme docker-compose).

Execute o script database/seed.sql (se disponível) ou os comandos SQL fornecidos para popular os produtos e imagens iniciais.

Aceder à Aplicação:

Frontend (Loja): Abra o ficheiro frontend/index.html no seu navegador ou use uma extensão como "Live Server".

Swagger (Documentação da API): http://localhost:7079/swagger

WireMock (Dashboard): http://localhost:9090/__admin

🧪 Como Testar (Guião Rápido)

Registo: Vá a Login > Sign up e crie uma conta.

Catálogo: Navegue para Products. Carregamento é rápido (Redis).

Teste de Integração: Clique no botão "Verificar Stock (Fornecedor)" num produto. O sistema irá consultar o WireMock e devolver a quantidade (ex: 150 un.).

Compra: Adicione produtos ao Cart, vá a Checkout e finalize a compra.

Confirmação: Vá a My Orders para ver a encomenda registada na base de dados.

## 📂 Estrutura do Repositório

O projeto está organizado da seguinte forma:

```text
ApiDiogoGoncaloProjetoFinal/       # Raiz do Projeto
│
├── Controllers/                   # Endpoints da API (Products, Auth, Orders)
├── Models/                        # Entidades da Base de Dados (Product, User...)
├── Data/                          # Configuração do Entity Framework (DbContext)
├── DTOs/                          # Objetos de Transferência de Dados
├── Program.cs                     # Configuração Principal (.NET 8, DI, Swagger)
│
├── frontend/                      # Aplicação Web (Cliente)
│   ├── css/                       # Folhas de estilo
│   ├── js/                        # Lógica Javascript (Fetch API)
│   ├── Pages/                     # Páginas HTML (Login, Catálogo, Checkout...)
│   └── assets/                    # Imagens dos produtos
│
├── database/                      # Scripts de Base de Dados
│   └── seed.sql                   # Script para popular produtos e imagens
│
├── imposter/                      # Configuração do WireMock
│   └── mappings/                  # Regras de resposta do fornecedor falso
│
├── docker-compose.yml             # Orquestração dos contentores (API, DB, Redis, WireMock)
└── README.md                      # Documentação do Projeto

Projeto desenvolvido no âmbito da UC00605, 2025.
