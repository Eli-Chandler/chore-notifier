backend:
	docker compose up -d postgres backend

db:
	docker compose up -d postgres

full:
	docker compose --profile frontend up

down:
	docker compose down
