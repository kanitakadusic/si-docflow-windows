# docflow-windows

## Application Settings Setup

> The variables described in this document should be adjusted according to the intended configuration. All other variables should retain their default values.

### ADMIN_SERVER_BASE_URL

The base URL of the admin server used by the Windows application to fetch configuration and send logs.  
Currently, there is one dedicated admin server instance available: [si-docflow-admin](https://github.com/HarisMalisevic/si-docflow-admin).

### PROCESSING_SERVER_BASE_URL

The base URL of the processing server used by the Windows application to handle document processing and finalization.  
Currently, there is one dedicated processing server instance available: [si-docflow-server](https://github.com/kanitakadusic/si-docflow-server).

### PORT

The port on which the Windows application listens for incoming document processing requests.

### MACHINE_ID

A unique identifier for the Windows application used to fetch its initial configuration from the admin server.  
It follows the format **ipAddress:port**.

### OPERATIONAL_MODE

Specifies the mode in which the Windows application will run if fetching the configuration from the admin server fails. There are two possible modes, shown below with their exact names:

- The application runs with the user interface (UI) enabled:
```
standalone
```

- The application runs without a UI but listens for document processing requests:
```
headless
```

### POLLING_FREQUENCY

Specifies the interval, in hours (integer), at which the application requests configuration updates from the admin server. This value can be updated if a different frequency is received from the admin server configuration.

### OCR_LANGUAGE

Specifies the language code (three-letter) for document processing, passed as a parameter during OCR.

### OCR_ENGINE

Specifies the OCR engine used for document processing. Supported values:

```
tesseract
```
```
googleVision
```
```
chatGpt
```
