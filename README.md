# docflow-windows

> document processing system

This repository contains the Windows application for a document processing system. It allows users to upload and process documents via OCR, review and correct extracted data, and submit the final version. The app automatically monitors a folder for new documents, sends them to a processing server, and handles user verification and corrections. This repo focuses on the desktop application, which interacts with separate repositories for the [processing server](https://github.com/kanitakadusic/si-docflow-server.git) and [admin dashboard](https://github.com/HarisMalisevic/si-docflow-admin.git).

## Architecture 🗂️

The component diagram of the system is provided below.

![System architecture](documentation/images/systemArchitecture.png)

## How to Use ⚙️

### Prerequisites

- [Git](https://git-scm.com/)
- [Visual Studio 2022](https://visualstudio.microsoft.com/downloads/) with the following workloads installed:
  - .NET desktop development (available through the Visual Studio Installer)
  - WinUI application development (available through the Visual Studio Installer)

### Steps

#### Environment Setup

1. Run Visual Studio 2022 **as administrator**.
> Running as administrator is required for the HTTP listener to function when the application is in `headless` operational mode. The application can still run without administrator privileges, but it will not be able to listen for document processing requests.
2. In the _Get started_ section, select _Clone a repository_.
3. In the _Repository location_ field, paste the following URL:
```
https://github.com/kanitakadusic/si-docflow-windows.git
```
4. Click the three dots next to the _Path_ field, choose an empty folder as the solution location, then click _Select Folder_.
5. Click _Clone_.

#### Configuration File Setup

In _Solution Explorer_, navigate to _Solution 'docflow'_ > _docflow_ > _appsettings.json_.  
Update _appsettings.json_ with your configuration. See [appsettings-setup.md](./documentation/appsettings-setup.md) for details.

#### Application Debug

1. Set the startup project to `docflow (Unpackaged)`.
2. Set the build configuration to `Debug`.
3. Run the application.

#### Application Publish (with required DLLs)

1. In _Solution Explorer_, right-click the _docflow_ project and select _Publish_.
2. Under _Target_, select _Folder_, then click _Next_.
3. Under _Specific target_, select _Folder_, then click _Next_.
4. Click _Browse_ next to the _Folder location_ field, choose an empty folder as the publish output location, then click _OK_.
5. Click _Finish_, then _Close_.
6. On the _docflow: Publish_ screen, click _Show all settings_.
7. In the _Configuration_ field, select `Release | <platform>`.
8. Set _Deployment mode_ to _Framework-dependent_.
9. Set _Target runtime_ to `win-<platform>`.
10. Click _Save_, then _Publish_.

## Documentation 📚

- [Application Settings Setup](./documentation/appsettings-setup.md)
- [API documentation](./documentation/api-docs.md)
- [Questions and Answers](./documentation/q&a.md)

## Instructional Videos 🎥

👉 [Click here to watch the local setup video](https://drive.google.com/file/d/1M04ggYMaDb_OXw_n5H-JcJ1_xzW92U1p/view?usp=sharing) (4 minutes 37 seconds)
