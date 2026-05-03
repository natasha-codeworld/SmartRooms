# SmartRooms — Meeting Room Booking System

A production-ready **microservices-based Meeting Room Booking API** built with ASP.NET Core 8, Entity Framework Core, SQL Server, Docker, and Ocelot API Gateway.

---

## Tech Stack

| Technology | Purpose |
|---|---|
| ASP.NET Core 8 Web API | REST API for all services |
| Entity Framework Core 8 | ORM + migrations |
| SQL Server 2022 | Database (separate DB per service) |
| Docker + Docker Compose | Containerisation |
| Ocelot | API Gateway / reverse proxy |
| JWT Bearer Tokens | Authentication |
| BCrypt | Password hashing |

---

## Architecture

```
Client
  └── API Gateway (:5000)   [Ocelot — single entry point]
        ├── /api/rooms     → RoomService     → RoomDb     (SQL Server)
        ├── /api/bookings  → BookingService  → BookingDb  (SQL Server)
        └── /api/auth      → UserService     → UserDb     (SQL Server)
```

Each service has its **own database** (database-per-service pattern).  
Services communicate internally over Docker network.

---

## Features

### Room Service
- List all rooms
- Get available rooms by date/time slot
- Create, update, delete rooms
- Seed data with 5 rooms on startup

### Booking Service
- Create a booking (with conflict detection)
- Cancel a booking
- View bookings by user or room
- Overlap/conflict prevention using SQL time-range queries
- Booking status tracking: Confirmed, Cancelled

### User Service
- Register a new user
- Login and receive a JWT token
- Password hashed with BCrypt
- JWT valid for 8 hours

---

## Quick Start

### Prerequisites
- Docker Desktop installed and running
- That's it — no .NET SDK needed to run

### Run the project

```bash
git clone https://github.com/YOUR_USERNAME/SmartRooms.git
cd SmartRooms
docker-compose up --build
```

Wait ~30 seconds for SQL Server to initialise, then:

| Endpoint | URL |
|---|---|
| API Gateway | http://localhost:5000 |
| Register user | POST http://localhost:5000/api/auth/register |
| Login | POST http://localhost:5000/api/auth/login |
| List rooms | GET http://localhost:5000/api/rooms |
| Available rooms | GET http://localhost:5000/api/rooms/available |
| Create booking | POST http://localhost:5000/api/bookings |
| My bookings | GET http://localhost:5000/api/bookings/user/{userId} |

---

## Sample API Calls

### Register
```json
POST /api/auth/register
{
  "name": "John Smith",
  "email": "john@example.com",
  "password": "Password123!"
}
```

### Login
```json
POST /api/auth/login
{
  "email": "john@example.com",
  "password": "Password123!"
}
// Returns: { "token": "eyJ..." }
```

### Create Booking
```json
POST /api/bookings
{
  "roomId": 1,
  "bookedByUserId": "1",
  "bookedByName": "John Smith",
  "startTime": "2025-06-01T09:00:00",
  "endTime": "2025-06-01T11:00:00",
  "title": "Sprint Planning",
  "notes": "Bring laptops"
}
```

### Conflict Example
Trying to book Room 1 for an overlapping time returns:
```json
HTTP 409 Conflict
{ "message": "Room is already booked for that time slot." }
```

---

## Key Design Decisions

- **Database per service**: Each microservice owns its own SQL Server database — no shared tables
- **Conflict detection**: SQL query checks `StartTime < newEnd AND EndTime > newStart` to catch all overlap scenarios
- **Service-to-service calls**: BookingService calls RoomService over HTTP to verify room existence
- **Migrations on startup**: Each service runs `db.Database.Migrate()` at startup — no manual migration step needed
- **Health checks**: Docker Compose waits for SQL Server to be healthy before starting services

---

## Author

Built by Natasha Sharma — (https://www.linkedin.com/in/natasha-sharma-60b2bb234/)
