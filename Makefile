.PHONY: up down build test lint docker-build

up:
	docker compose up -d

down:
	docker compose down

build:
	dotnet build VetCare.sln

test:
	dotnet test VetCare.sln

lint:
	dotnet format VetCare.sln --verify-no-changes

docker-build:
	docker build -t vetcare-api .
