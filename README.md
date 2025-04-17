# docflow-windows

> document processing system

This repository contains the Windows application for a document processing system. It allows users to upload and process documents via OCR, review and correct extracted data, and submit the final version. The app automatically monitors a folder for new documents, sends them to a processing server, and handles user verification and corrections. This repo focuses on the desktop application, which interacts with separate repositories for the [processing server](https://github.com/kanitakadusic/si-docflow-server.git) and [admin dashboard](https://github.com/HarisMalisevic/si-docflow-admin.git).

## Architecture 🗂️

The component diagram of the system is provided below.

![System architecture](documentation/images/systemArchitecture.png)

## How to Use ⚙️

### Prerequisites:

- Git
- Visual Studio 2022 with the following workloads installed:
  - .NET desktop development
  - WinUI application development

### Steps:

1. Clone the repository
2. Open the `docflow.sln` solution file in Visual Studio
3. Configure build settings:
   - Set **Configuration** to `Debug`
   - Select **Startup Project**: `docflow (Unpackaged)`
4. Run the application
