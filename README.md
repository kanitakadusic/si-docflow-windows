# docflow-windows

> document processing system

This repository contains the Windows application for a document processing system. It allows users to upload and process documents via OCR, review and correct extracted data, and submit the final version. The app automatically monitors a folder for new documents, sends them to a processing server, and handles user verification and corrections. This repo focuses on the desktop application, which interacts with separate repositories for the [processing server](https://github.com/kanitakadusic/si-docflow-server.git) and [admin dashboard](https://github.com/HarisMalisevic/si-docflow-admin.git).

## Architecture 🗂️

The component diagram of the system is provided below.

![System architecture](documentation/images/systemArchitecture.png)

## How to Use ⚙️

### Prerequisites:

- [Git](https://git-scm.com/)
- [Visual Studio 2022](https://visualstudio.microsoft.com/downloads/) with the following workloads installed:
  - .NET desktop development (available through the Visual Studio Installer)
  - WinUI application development (available through the Visual Studio Installer)

### Steps:

1. Clone the repository.

2. Update _appsettings.json_ with your configuration. Instructions are in [appsettings-setup.md](./documentation/appsettings-setup.md).

3. Open the _docflow.sln_ file in Visual Studio.

4. Set configuration to `Debug`.

5. Select `docflow (Unpackaged)` startup project.

6. Run the application.

## Documentation 📚

- [Application Settings Setup](./documentation/appsettings-setup.md)
- [Questions and Answers](./documentation/q&a.md)

## Instructional Videos 🎥

👉 [Click here to watch the local setup video](https://drive.google.com/file/d/1M04ggYMaDb_OXw_n5H-JcJ1_xzW92U1p/view?usp=sharing) (4 minutes 37 seconds)
