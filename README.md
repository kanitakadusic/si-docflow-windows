# docflow-windows

> document processing system

This repository contains the Windows application for a document processing system. It allows users to upload and process documents via OCR, review and correct extracted data, and submit the final version. The app automatically monitors a folder for new documents, sends them to a processing server, and handles user verification and corrections. This repo focuses on the desktop application, which interacts with separate repositories for the [processing server](https://github.com/kanitakadusic/si-docflow-server.git) and [admin dashboard](https://github.com/HarisMalisevic/si-docflow-admin.git).

## Architecture 🗂️

The component diagram of the system is provided below.

![System architecture](images/systemArchitecture.png)

## How To Use ⚙️

To clone and run this application, you will need [Git](https://git-scm.com/) and [Node.js](https://nodejs.org/).

```
# Clone this repository
$ git clone https://github.com/kanitakadusic/si-docflow-windows.git

# Go into the root directory
$ cd si-docflow-windows

# Install all dependencies
$ npm install

# Run the app in dev mode
$ npm run dev
```
