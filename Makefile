backend:
	docker compose up -d postgres backend

db:
	docker compose up -d postgres

full:
	docker compose --profile frontend up

down:
	docker compose down

api-client:
	dotnet build
	cd services/frontend && npm run generate:api
