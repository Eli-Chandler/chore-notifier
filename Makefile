backend:
	docker compose up -d postgres backend

full:
	docker compose --profile frontend up

down:
	docker compose down
