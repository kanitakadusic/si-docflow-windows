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

### Publishing the Application

1. Clone the repository.

2. Open _docflow.sln_ in Visual Studio.

3. In _Solution Explorer_, right-click the _docflow_ **project** and select _Publish_.

4. Under _Target_, select _Folder_ and click _Next_.

5. Under _Specific target_ select _Folder_ and click _Next_.

6. Choose the output folder and click _Finish_, then _Close_.

7. On the main Publish screen, click _Show all settings_.

8. In the _Configuration_ field, select:
    - `Release | x64` for modern 64-bit systems
    - `Release | x86` for older 32-bit systems
    - `Release | ARM64` if your processor is ARM-based
   
9. Set _Deployment mode_ to _Framework-dependent_.

10. Set _Target runtime_ based on the configuration:
    - `win-x64` for 64-bit Windows
    - `win-x86` for 32-bit Windows
    - `win-arm64` for ARM
   
11. Click _Save_, then _Publish_.

12. The application will be published to the selected folder with all required DLLs.

### Debugging the Application

1. Clone the repository.

2. Open _docflow.sln_ in Visual Studio **as Administrator**.

3. Set the startup project to `docflow (Unpackaged)`.

4. Set the configuration to `Debug`.

5. Update _appsettings.json_ with your configuration. Instructions are in [appsettings-setup.md](./documentation/appsettings-setup.md).

6. Run the application.

## Documentation 📚

- [Application Settings Setup](./documentation/appsettings-setup.md)
- [Questions and Answers](./documentation/q&a.md)

## Instructional Videos 🎥

👉 [Click here to watch the local setup video](https://drive.google.com/file/d/1M04ggYMaDb_OXw_n5H-JcJ1_xzW92U1p/view?usp=sharing) (4 minutes 37 seconds)
