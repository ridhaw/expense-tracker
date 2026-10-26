# Suivi des dépenses — application full-stack

[![CI](https://github.com/ridhaw/expense-tracker/actions/workflows/ci.yml/badge.svg)](https://github.com/ridhaw/expense-tracker/actions/workflows/ci.yml)

Application web de suivi de dépenses personnelles : inscription et connexion, saisie des
dépenses par catégorie, filtrage par mois et synthèse mensuelle par catégorie.

| Couche | Technologies |
|---|---|
| **API** | ASP.NET Core 10 (Minimal APIs), Entity Framework Core 10, authentification JWT |
| **Base de données** | PostgreSQL 17, migrations EF Core, contrainte `CHECK` sur les montants |
| **Frontend** | Next.js 16 (App Router), React 19, TypeScript, Tailwind CSS 4 |
| **Qualité** | 16 tests d'intégration xUnit sur un vrai PostgreSQL (Testcontainers), ESLint |
| **Livraison** | Docker multi-étapes, Docker Compose, GitHub Actions, blueprint Render |

## Fonctionnalités

- **Comptes** : mots de passe hachés (PBKDF2 via `PasswordHasher` d'ASP.NET Core Identity),
  jetons JWT signés HMAC-SHA256 avec expiration ; même message d'erreur pour un email inconnu
  et un mauvais mot de passe, pour ne pas révéler quels comptes existent.
- **Dépenses** : création, modification, suppression, liste filtrée par mois.
- **Isolation** : chaque requête est filtrée sur l'utilisateur du jeton — un utilisateur ne
  peut ni lire ni modifier les dépenses d'un autre (vérifié par les tests).
- **Synthèse mensuelle** : total, nombre de dépenses et répartition par catégorie, calculés
  en SQL (`GROUP BY`).
- **Validation** : email, longueur du mot de passe, libellé, montant strictement positif
  (validation côté API + contrainte en base) ; erreurs au format Problem Details (RFC 9457).

## API

| Méthode | Route | Rôle |
|---|---|---|
| `POST` | `/api/auth/register` | créer un compte, renvoie un JWT |
| `POST` | `/api/auth/login` | se connecter, renvoie un JWT |
| `GET` | `/api/expenses?month=AAAA-MM` | lister les dépenses (du mois) |
| `GET` | `/api/expenses/summary?month=AAAA-MM` | synthèse du mois par catégorie |
| `GET` `PUT` `DELETE` | `/api/expenses/{id}` | consulter, modifier, supprimer |
| `POST` | `/api/expenses` | ajouter une dépense |
| `GET` | `/health` · `/openapi/v1.json` | santé · spécification OpenAPI |

## Lancer en local

**Avec Docker** (base, API et frontend) :

```bash
echo "JWT_KEY=$(openssl rand -base64 48)" > .env
docker compose up --build
```

Frontend : http://localhost:3000 · API : http://localhost:8080

**Sans Docker pour le code** (PostgreSQL seul dans Docker) :

```bash
docker run -d -p 5432:5432 -e POSTGRES_DB=expenses -e POSTGRES_USER=expenses -e POSTGRES_PASSWORD=expenses postgres:17-alpine
dotnet run --project backend/ExpenseTracker.Api      # migrations appliquées au démarrage
cd frontend && npm install && NEXT_PUBLIC_API_URL=http://localhost:5068 npm run dev
```

## Tests

```bash
dotnet test backend/ExpenseTracker.slnx
```

Les tests démarrent l'API en mémoire (`WebApplicationFactory`) sur une base PostgreSQL
éphémère lancée dans Docker par Testcontainers : inscription et connexion, validation,
CRUD, isolation entre utilisateurs, synthèse mensuelle, filtrage par mois, conversion des
URL de connexion.

## Intégration continue

À chaque push, GitHub Actions lance les tests du backend, le lint et le build du frontend,
puis construit toute la pile avec Docker Compose et vérifie qu'un compte peut être créé et
que le frontend répond.

## Déployer

Le fichier [`render.yaml`](render.yaml) décrit le déploiement sur [Render](https://render.com)
(offre gratuite) : base PostgreSQL, API et frontend. Dans Render : **New → Blueprint**, choisir
ce dépôt, puis renseigner `Cors__Origins__0` (URL du frontend) et `NEXT_PUBLIC_API_URL`
(URL de l'API). La clé JWT est générée automatiquement.

## Structure

```
backend/
  ExpenseTracker.Api/
    Domain/        entités User, Expense, catégories
    Data/          DbContext, migrations, conversion des URL PostgreSQL
    Auth/          émission des JWT
    Endpoints/     routes, contrats, validation
  ExpenseTracker.Api.Tests/   tests d'intégration
frontend/src/
  app/login/       connexion et inscription
  app/dashboard/   synthèse, formulaire, liste
  lib/api.ts       client de l'API
```
