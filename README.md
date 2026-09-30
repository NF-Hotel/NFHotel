# 🏨 NF Hotel

NF Hotel is a web-based hotel management system developed as part of a software development project. The system is designed to improve the management of hotel bookings, guests, rooms, pricing, and check-in workflows.

The project is based on the requirements gathered during the **NF Hotel 1.0 process** and focuses on creating a more efficient and user-friendly solution for hotel staff.

The system is being developed with **Blazor**, **PostgreSQL**, and a web API architecture, with the goal of supporting both internal hotel management and future integrations with external booking platforms.

---

## 📑 Table of Contents

* [🏨 NF Hotel](#-nf-hotel)
* [🎯 Project Goals](#-project-goals)
* [✨ Customer Requirements](#-customer-requirements)

  * [🛂 Passport Details](#-passport-details)
  * [🚶 Walk-in Guests](#-walk-in-guests)
  * [📅 Calendar Improvements](#-calendar-improvements)
  * [👤 Guest Overview](#-guest-overview)
  * [💰 Special Pricing & Discounts](#-special-pricing--discounts)
  * [🌐 Web-based Solution](#-web-based-solution)
* [🛠️ Technology Stack](#️-technology-stack)
* [📋 Prerequisites](#-prerequisites)
* [🚀 How To Get Started](#-how-to-get-started)
* [⚙️ Configuration](#️-configuration)
* [🗄️ Database](#️-database)
* [🔌 API](#-api)
* [👥 Development Team](#-development-team)
* [🔐 Security & Privacy](#-security--privacy)
* [📌 Project Status](#-project-status)
* [📄 License](#-license)

---

## 🎯 Project Goals

The main goal of NF Hotel is to develop a modern hotel management solution that makes everyday hotel operations easier and more efficient.

The system focuses on:

* Managing hotel bookings
* Managing guest information
* Connecting guests with their assigned rooms
* Supporting walk-in guests
* Managing passport information for check-in
* Managing room prices and discounts
* Providing a clear and usable booking calendar
* Providing sales and pricing overviews
* Providing an API for future integrations
* Creating a foundation for integration with external booking platforms

---

## ✨ Customer Requirements

The following requirements are based on the customer's wishes from the **NF Hotel 1.0 process**.

### 🛂 Passport Details

The system should support passport information as part of the guest and booking workflow.

Requirements:

* Add a **Passport ID** field to guest information.
* Allow hotel staff to upload an image of the guest's passport.
* Keep the following information together:

  * Booking
  * Guest
  * Passport ID
  * Passport image
* Make passport information easily accessible during check-in.

This is intended to make the check-in process faster and reduce the need to search for information across multiple systems.

---

### 🚶 Walk-in Guests

Hotel guests who arrive without a prior booking or without an email address should still be able to be registered.

Requirements:

* Remove the mandatory email requirement from **New Booking**.
* Allow bookings to be created without an email address.
* Support hotel staff entering guest information manually.

This is particularly important for walk-in guests who may not have an email address available.

---

### 📅 Calendar Improvements

The booking calendar should provide a clearer overview of hotel occupancy and booking status.

Requirements:

* Highlight the booking that has been clicked or selected.
* Provide a clear visual indication of the selected booking.
* Add a **color legend** explaining the different booking statuses.
* Make it easier for hotel staff to understand the current booking situation.

The goal is to make the calendar easier and faster to use during everyday hotel operations.

---

### 👤 Guest Overview

The guest overview should make it easy to identify where a guest is staying.

Requirements:

* Display the guest's assigned room number.
* Provide clear **guest ↔ room traceability**.
* Make the assigned room visible from the relevant guest information.

This allows hotel staff to quickly determine which room belongs to a specific guest.

---

### 💰 Special Pricing & Discounts

The system should support special pricing for selected hotel rooms.

Requirements:

* Add discount pricing for ect **rooms 8, 9, and 10**.
* Make the discounted prices available when editing room prices.
* Reflect pricing changes in the **Sales Overview**.
* Ensure that the displayed prices are consistent throughout the system.

---

### 🌐 Web-based Solution

NF Hotel is being developed as a web-based solution rather than a traditional desktop-only application.

The system should provide:

* A web-based user interface.
* A backend API.
* Database integration.
* Support for website bookings.
* A foundation for integration with external booking platforms.
* A scalable architecture that can be extended in the future.

The API architecture is intended to make it possible for external systems to communicate with NF Hotel.

---

## 🛠️ Technology Stack

NF Hotel uses the following technologies:

| Technology        | Purpose                                              |
| ----------------- | ---------------------------------------------------- |
| **Blazor**        | Web application and user interface                   |
| **PostgreSQL**    | Relational database                                  |
| **.NET / C#**     | Application and backend development                  |
| **Web API**       | Communication with external systems and integrations |
| **Visual Studio** | Primary development IDE                              |

### 🧩 Architecture

The system is designed around a web application and API architecture:

```text
                    ┌─────────────────────┐
                    │   External Systems  │
                    │ Website / Platforms │
                    └──────────┬──────────┘
                               │
                               ▼
                    ┌─────────────────────┐
                    │       Web API       │
                    └──────────┬──────────┘
                               │
                ┌──────────────┴──────────────┐
                │                             │
                ▼                             ▼
       ┌─────────────────┐          ┌─────────────────┐
       │   Blazor WebApp │          │   PostgreSQL    │
       │   Hotel UI      │          │    Database     │
       └─────────────────┘          └─────────────────┘
```

---

## 📋 Prerequisites

Before running NF Hotel locally, make sure you have the following installed.

### 💻 Development Environment

* **Visual Studio 2022** or newer
* **.NET SDK** compatible with the project's target framework
* **PostgreSQL**
* **Git**

Visual Studio should have the appropriate **ASP.NET and web development** workload installed.

### 🔧 Recommended

For development, it is also recommended to have:

* PostgreSQL administration tool such as pgAdmin
* GitHub account
* A database management tool
* Access to the required project configuration and environment variables

---

## 🚀 How To Get Started

### 1️⃣ Clone the Repository

Clone the project from GitHub:

```bash
git clone https://github.com/NF-Hotel/NFHotel.git
```

Navigate into the project:

```bash
cd NFHotel
```

---

### 2️⃣ Open the Project

Open the solution in **Visual Studio**.

Locate the `.sln` file and open it.

Alternatively, the project can be opened directly through Visual Studio using:

```text
File → Open → Project/Solution
```

---

### 3️⃣ Configure PostgreSQL

Create a PostgreSQL database for NF Hotel.

For example:

```text
Database: nfhotel
```

The exact database name and connection settings should match the project's configuration.

Configure the database connection using the appropriate application configuration, such as:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=nfhotel;Username=YOUR_USERNAME;Password=YOUR_PASSWORD"
  }
}
```

**Do not commit real passwords, API keys, or other secrets to GitHub.**

---

### 4️⃣ Restore Dependencies

From the project directory:

```bash
dotnet restore
```

Build the project:

```bash
dotnet build
```

---

### 5️⃣ Set Up the Database

If the project uses Entity Framework Core migrations, apply the migrations using:

```bash
dotnet ef database update
```

If Entity Framework CLI is not installed:

```bash
dotnet tool install --global dotnet-ef
```

Then run:

```bash
dotnet ef database update
```

> The exact database setup may depend on the current implementation of the project.

---

### 6️⃣ Run the Application

The application can be started from Visual Studio using:

```text
F5
```

or:

```text
Ctrl + F5
```

It can also be started from the terminal with:

```bash
dotnet run
```

Visual Studio will provide the local URL where the application is running.

---

## ⚙️ Configuration

Configuration should be kept outside the source code whenever possible.

Typical configuration includes:

* PostgreSQL connection string
* API configuration
* Authentication settings
* External service credentials
* File/image storage configuration
* Environment-specific settings

### 🔐 Environment Variables

Sensitive values should preferably be supplied through environment variables or local development configuration.

For example:

```text
DATABASE_HOST
DATABASE_PORT
DATABASE_NAME
DATABASE_USER
DATABASE_PASSWORD
```

Never commit production credentials or sensitive customer data to the repository.

---

## 🗄️ Database

NF Hotel uses **PostgreSQL** as its relational database.

The database is responsible for storing information such as:

* Guests
* Bookings
* Rooms
* Room prices
* Discounts
* Passport IDs
* Passport image references
* Booking status
* Sales information

The database structure should maintain clear relationships between guests, bookings, and rooms.

A simplified relationship can be represented as:

```text
Guest
  │
  ├── Passport ID
  ├── Passport Image
  │
  └── Booking
        │
        ├── Check-in
        ├── Check-out
        ├── Status
        └── Room
              │
              ├── Room Number
              └── Price
```

---

## 🔌 API

NF Hotel includes a web API architecture intended to support communication between the hotel system and external applications.

Potential integrations include:

* Hotel website
* Online booking systems
* External booking platforms
* Third-party services
* Future mobile applications

The API provides a foundation for extending NF Hotel without requiring external applications to access the PostgreSQL database directly.

---

## 👥 Development Team

NF Hotel is developed by a team with different areas of responsibility.

| Team Member                           | Responsibility             |
| ------------------------------------- | -------------------------- |
| **Michael Kragh**                     | 🔐 Cyber Security & DevOps |
| **William Bøgh Christensen**          | 💻 Software Development    |
| **Jakob Kelvin Jensen**               | 📱 Mobile Development      |
| **Valdemar Louis Vesterdal Carlsson** | 🎨 Frontend & AI           |
| **Jens Tirsvad Nielsen**              | 📊 Data Analysis & AI      |

The project uses a collaborative development approach where the team contributes across different areas of the solution.

---

## 🔐 Security & Privacy

NF Hotel handles potentially sensitive hotel and guest information.

Examples include:

* Guest information
* Passport IDs
* Passport images
* Booking information
* Hotel rates
* Sales information
* API data

Security and privacy are therefore important considerations throughout the development process.

Particular attention should be given to:

* Authentication and authorization
* Secure API endpoints
* Database security
* Input validation
* Secure file uploads
* Protection of passport information
* Protection of credentials and secrets
* Access control
* Secure handling of customer data

Sensitive information should never be committed to the Git repository.

---

## 📌 Project Status

🚧 **NF Hotel is currently under development.**

The project is being developed based on the requirements from the **NF Hotel 1.0 process**.

Current development areas include:

* 🏨 Hotel management
* 📅 Booking management
* 👤 Guest management
* 🛂 Passport information
* 🚪 Check-in workflow
* 💰 Room pricing and discounts
* 📊 Sales overview
* 🌐 Web application
* 🔌 API integrations
* 🤖 AI and data analysis

Features and architecture may change as development progresses.

---

## 📄 License

This project is developed as part of an educational software development project.

Unless otherwise specified, the project should not be considered licensed for commercial redistribution or reuse.

For questions regarding the project or its use, please contact the development team.

---

## 🔗 Repository

The source code is available on GitHub:

**https://github.com/NF-Hotel/NFHotel**
